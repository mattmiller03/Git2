using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using System.Linq;
using System.Threading;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.Services
{
    public class ConnectionManager
    {
        private readonly ILogger<ConnectionManager> _logger;
        private readonly IProfileManager _profileManager;
        private readonly ICredentialManager _credentialManager;
        private readonly PowerShellManager _powerShellManager;
        private readonly Dictionary<string, ConnectionResult> _connectionCache = new();
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

        public ObservableCollection<ConnectionProfile> ServerProfiles { get; } = new();
        public ConnectionProfile? CurrentConnection { get; private set; }
        public bool IsConnected => CurrentConnection != null && _connectionCache.ContainsKey(CurrentConnection.ServerAddress);

        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        public ConnectionManager(
            ILogger<ConnectionManager> logger,
            IProfileManager profileManager,
            ICredentialManager credentialManager,
            PowerShellManager powerShellManager)
        {
            _logger = logger;
            _profileManager = profileManager;
            _credentialManager = credentialManager;
            _powerShellManager = powerShellManager;
            LoadProfiles();
        }

        /// <summary>
        /// Test connection to vCenter server with timeout
        /// </summary>
        public async Task<ConnectionResult> TestConnectionAsync(ConnectionProfile profile, int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            try
            {
                await _connectionSemaphore.WaitAsync(linkedCts.Token);
                try
                {
                    _logger.LogInformation("Testing connection to {Server} with {Timeout}s timeout",
                        profile.ServerAddress, timeoutSeconds);

                    var securePassword = _credentialManager.GetPassword(profile.Name);
                    if (securePassword.Length == 0)
                    {
                        var result = new ConnectionResult(false, errorMessage: "No saved credentials found");
                        _logger.LogWarning("No credentials found for profile {ProfileName}", profile.Name);
                        return result;
                    }

                    var credential = new PSCredential(profile.Username, securePassword);

                    // Pass the cancellation token to the PowerShell manager
                    var connectionResult = await _powerShellManager.TestVCenterConnectionAsync(
                        profile, credential, linkedCts.Token);

                    // Cache successful connections
                    if (connectionResult.IsSuccessful)
                    {
                        _connectionCache[profile.ServerAddress] = connectionResult;
                        profile.LastConnected = DateTime.Now; // Update last connected time
                        _logger.LogInformation("Successfully connected to {Server}, version: {Version}",
                            profile.ServerAddress, connectionResult.Version);
                    }
                    else
                    {
                        _connectionCache.Remove(profile.ServerAddress);
                        _logger.LogError("Failed to connect to {Server}: {Error}",
                            profile.ServerAddress, connectionResult.ErrorMessage);
                    }

                    return connectionResult;
                }
                finally
                {
                    _connectionSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Connection test to {Server} timed out after {Timeout}s",
                        profile.ServerAddress, timeoutSeconds);
                    return new ConnectionResult(false, errorMessage: $"Connection timed out after {timeoutSeconds} seconds");
                }

                _logger.LogInformation("Connection test to {Server} was cancelled", profile.ServerAddress);
                return new ConnectionResult(false, errorMessage: "Connection test cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed for {Server}", profile.ServerAddress);
                return new ConnectionResult(false, errorMessage: ex.Message);
            }
        }
        /// <summary>
        /// Check if a server address already exists in any profile
        /// </summary>
        public bool ServerExists(string serverAddress)
        {
            return ServerProfiles.Any(p =>
                string.Equals(p.ServerAddress, serverAddress, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Update profile with option to transfer credentials
        /// </summary>
        public async Task<bool> UpdateProfileAsync(string oldProfileName, ConnectionProfile newProfile, bool transferCredentials)
        {
            try
            {
                var oldProfile = GetProfileByName(oldProfileName);
                if (oldProfile == null)
                {
                    _logger.LogWarning("Cannot update profile: original profile {ProfileName} not found", oldProfileName);
                    return false;
                }

                // First update the profile in storage
                _profileManager.SaveProfile(newProfile);

                // Transfer credentials if requested
                if (transferCredentials && oldProfile.Name != newProfile.Name)
                {
                    var password = _credentialManager.GetPassword(oldProfile.Name);
                    if (password.Length > 0)
                    {
                        _credentialManager.SavePassword(newProfile.Name, newProfile.Username, password);
                        _credentialManager.DeletePassword(oldProfile.Name);
                    }
                }

                // If the name changed, delete the old profile
                if (oldProfile.Name != newProfile.Name)
                {
                    _profileManager.DeleteProfile(oldProfile.Name);
                }

                // Reload profiles and update cache references
                LoadProfiles();

                if (_connectionCache.TryGetValue(oldProfile.ServerAddress, out var cachedResult))
                {
                    if (oldProfile.ServerAddress != newProfile.ServerAddress)
                    {
                        _connectionCache.Remove(oldProfile.ServerAddress);
                        _connectionCache[newProfile.ServerAddress] = cachedResult;
                    }
                }

                // If this was the current connection, update the reference
                if (CurrentConnection?.Name == oldProfile.Name)
                {
                    CurrentConnection = newProfile;
                }

                _logger.LogInformation("Profile {OldName} updated to {NewName}", oldProfile.Name, newProfile.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update profile {ProfileName}", oldProfileName);
                return false;
            }
        }

        private ConnectionProfile? GetProfileByName(string name)
        {
            return ServerProfiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>
        /// Connect to vCenter server and set as current connection
        /// </summary>
        public async Task<ConnectionResult> ConnectAsync(ConnectionProfile profile)
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Connecting to {Server}", profile.ServerAddress);

                var result = await TestConnectionAsync(profile);

                if (result.IsSuccessful)
                {
                    CurrentConnection = profile;
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(true, profile, result.Version));
                    _logger.LogInformation("Successfully established connection to {Server}", profile.ServerAddress);
                }
                else
                {
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, profile, null, result.ErrorMessage));
                }

                return result;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// Disconnect from current vCenter server
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                if (CurrentConnection == null)
                {
                    _logger.LogWarning("No active connection to disconnect");
                    return false;
                }

                _logger.LogInformation("Disconnecting from {Server}", CurrentConnection.ServerAddress);

                var disconnectResult = await _powerShellManager.DisconnectVCenterAsync(CurrentConnection);

                if (disconnectResult)
                {
                    _connectionCache.Remove(CurrentConnection.ServerAddress);
                    var previousConnection = CurrentConnection;
                    CurrentConnection = null;
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, previousConnection, null));
                    _logger.LogInformation("Successfully disconnected from {Server}", previousConnection.ServerAddress);
                }

                return disconnectResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnect");
                return false;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// Get cached connection result for a server
        /// </summary>
        public ConnectionResult? GetCachedConnection(string serverAddress)
        {
            return _connectionCache.TryGetValue(serverAddress, out var result) ? result : null;
        }

        /// <summary>
        /// Validate that current connection is still active
        /// </summary>
        public async Task<bool> ValidateCurrentConnectionAsync()
        {
            if (CurrentConnection == null)
                return false;

            var result = await TestConnectionAsync(CurrentConnection);
            return result.IsSuccessful;
        }

        public void LoadProfiles()
        {
            try
            {
                ServerProfiles.Clear();
                foreach (var profile in _profileManager.GetAllProfiles())
                {
                    ServerProfiles.Add(profile);
                }
                _logger.LogInformation("Loaded {Count} profiles", ServerProfiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profiles");
            }
        }

        public void AddProfile(ConnectionProfile profile)
        {
            try
            {
                _profileManager.SaveProfile(profile);
                LoadProfiles();
                _logger.LogInformation("Added profile: {ProfileName}", profile.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add profile");
                throw;
            }
        }

        public void RemoveProfile(string profileName)
        {
            try
            {
                _profileManager.DeleteProfile(profileName);
                _credentialManager.DeletePassword(profileName);
                LoadProfiles();
                _logger.LogInformation("Removed profile: {ProfileName}", profileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove profile");
                throw;
            }
        }

        public void Dispose()
        {
            _connectionSemaphore?.Dispose();
        }

        /// <summary>
        /// Event arguments for connection status changes
        /// </summary>
        public class ConnectionStatusChangedEventArgs(bool isConnected, ConnectionProfile profile, string? version = null, string? errorMessage = null) : EventArgs
        {
            public bool IsConnected { get; } = isConnected;
            public ConnectionProfile Profile { get; } = profile;
            public string? Version { get; } = version;
            public string? ErrorMessage { get; } = errorMessage;
        }

        public async Task<ConnectionResult> TestConnectionWithTimeoutAsync(ConnectionProfile profile, int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            try
            {
                await _connectionSemaphore.WaitAsync(linkedCts.Token);
                try
                {
                    _logger.LogInformation("Testing connection to {Server} with {Timeout}s timeout",
                        profile.ServerAddress, timeoutSeconds);

                    var securePassword = _credentialManager.GetPassword(profile.Name);
                    if (securePassword.Length == 0)
                    {
                        var result = new ConnectionResult(false, errorMessage: "No saved credentials found");
                        _logger.LogWarning("No credentials found for profile {ProfileName}", profile.Name);
                        return result;
                    }

                    var credential = new PSCredential(profile.Username, securePassword);

                    // Pass the cancellation token to the PowerShell manager
                    var connectionResult = await _powerShellManager.TestVCenterConnectionAsync(
                        profile, credential, linkedCts.Token);

                    // Cache successful connections
                    if (connectionResult.IsSuccessful)
                    {
                        _connectionCache[profile.ServerAddress] = connectionResult;
                        profile.LastConnected = DateTime.Now; // Update last connected time
                        _logger.LogInformation("Successfully connected to {Server}, version: {Version}",
                            profile.ServerAddress, connectionResult.Version);
                    }
                    else
                    {
                        _connectionCache.Remove(profile.ServerAddress);
                        _logger.LogError("Failed to connect to {Server}: {Error}",
                            profile.ServerAddress, connectionResult.ErrorMessage);
                    }

                    return connectionResult;
                }
                finally
                {
                    _connectionSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Connection test to {Server} timed out after {Timeout}s",
                        profile.ServerAddress, timeoutSeconds);
                    return new ConnectionResult(false, errorMessage: $"Connection timed out after {timeoutSeconds} seconds");
                }

                _logger.LogInformation("Connection test to {Server} was cancelled", profile.ServerAddress);
                return new ConnectionResult(false, errorMessage: "Connection test cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed for {Server}", profile.ServerAddress);
                return new ConnectionResult(false, errorMessage: ex.Message);
            }
        }
        /// <summary>
        /// Test connection with automatic retry for transient errors
        /// </summary>
        public async Task<ConnectionResult> TestConnectionWithRetryAsync(
            ConnectionProfile profile,
            int maxRetries = 3,
            int initialDelayMs = 500,
            CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            int delayMs = initialDelayMs;

            while (true)
            {
                var result = await TestConnectionAsync(profile, 15, cancellationToken);

                if (result.IsSuccessful || retryCount >= maxRetries || !IsTransientError(result.ErrorMessage))
                {
                    return result;
                }

                _logger.LogWarning("Transient error connecting to {Server}, retrying in {DelayMs}ms (attempt {RetryCount}/{MaxRetries}): {Error}",
                    profile.ServerAddress, delayMs, retryCount + 1, maxRetries, result.ErrorMessage);

                try
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Connection retry cancelled for {Server}", profile.ServerAddress);
                    return result;
                }

                retryCount++;
                delayMs *= 2; // Exponential backoff
            }
        }

        /// <summary>
        /// Determines if an error is likely transient and should be retried
        /// </summary>
        private bool IsTransientError(string? errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return false;

            // Common transient error messages - add more based on experience
            return errorMessage.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("connection refused", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("network error", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("too many connections", StringComparison.OrdinalIgnoreCase);
        }
    }
}


