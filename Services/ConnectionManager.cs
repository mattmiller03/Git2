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
        /// Test connection to vCenter server
        /// </summary>
        public async Task<ConnectionResult> TestConnectionAsync(ConnectionProfile profile)
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Testing connection to {Server}", profile.ServerAddress);

                var securePassword = _credentialManager.GetPassword(profile.Name);
                if (securePassword.Length == 0)
                {
                    var result = new ConnectionResult(false, errorMessage: "No saved credentials found");
                    _logger.LogWarning("No credentials found for profile {ProfileName}", profile.Name);
                    return result;
                }

                var credential = new PSCredential(profile.Username, securePassword);
                var connectionResult = await _powerShellManager.TestVCenterConnectionAsync(profile, credential);

                // Cache successful connections
                if (connectionResult.IsSuccessful)
                {
                    _connectionCache[profile.ServerAddress] = connectionResult;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed for {Server}", profile.ServerAddress);
                return new ConnectionResult(false, errorMessage: ex.Message);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
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
}
