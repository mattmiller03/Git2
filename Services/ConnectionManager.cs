using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.Services
{
    public class ConnectionManager
    {
        private readonly PowerShellManager _powerShellManager;
        private readonly IProfileManager _profileManager;
        private readonly ILogger<ConnectionManager> _logger;

        public ObservableCollection<ConnectionProfile> ServerProfiles { get; } = new();

        public ConnectionProfile? SelectedSourceProfile { get; set; }

        public ConnectionProfile? SelectedDestinationProfile { get; set; }

        public ConnectionManager(
            PowerShellManager powerShellManager,
            IProfileManager profileManager,
            ILogger<ConnectionManager> logger)
        {
            _powerShellManager = powerShellManager ??
                throw new ArgumentNullException(nameof(powerShellManager));
            _profileManager = profileManager ??
                throw new ArgumentNullException(nameof(profileManager));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            LoadProfiles();
        }

        private void LoadProfiles()
        {
            try
            {
                ServerProfiles.Clear();
                var profiles = _profileManager.GetAllProfiles();

                foreach (var profile in profiles)
                {
                    ServerProfiles.Add(profile);
                }

                _logger.LogInformation($"Loaded {ServerProfiles.Count} connection profiles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading connection profiles");
            }
        }

        public async Task<ConnectionResult> TestConnectionAsync(ConnectionProfile profile)
        {
            try
            {
                _logger.LogInformation($"Testing connection for profile: {profile.Name}");

                // Use PowerShellManager to test connection
                var result = await _powerShellManager.TestVCenterConnectionAsync(profile);

                _logger.LogInformation($"Connection test result for {profile.Name}: {result.IsSuccessful}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Connection test failed for profile: {profile.Name}");

                return new ConnectionResult(
                    isSuccessful: false,
                    errorMessage: $"Connection test failed: {ex.Message}"
                );
            }
        }

        public void AddProfile(ConnectionProfile profile)
        {
            try
            {
                _profileManager.SaveProfile(profile);
                LoadProfiles();

                _logger.LogInformation($"Added new connection profile: {profile.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding profile: {profile.Name}");
                throw;
            }
        }

        public void RemoveProfile(string profileName)
        {
            try
            {
                _profileManager.DeleteProfile(profileName);
                LoadProfiles();

                _logger.LogInformation($"Removed connection profile: {profileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing profile: {profileName}");
                throw;
            }
        }

        public ConnectionProfile? GetProfileByName(string profileName)
        {
            return ServerProfiles.FirstOrDefault(p => p.Name == profileName);
        }
    }
}
