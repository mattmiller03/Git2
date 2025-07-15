using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;

namespace UiDesktopApp2.Services
{
    public class ConnectionManager
    {
        private readonly ILogger<ConnectionManager> _logger;
        private readonly IProfileManager _profileManager;
        private readonly ICredentialManager _credentialManager;
        private readonly PowerShellManager _powerShellManager;

        public ObservableCollection<ConnectionProfile> ServerProfiles { get; } = new();

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

        public async Task<ConnectionResult> TestConnectionAsync(ConnectionProfile profile)
        {
            try
            {
                var securePassword = _credentialManager.GetPassword(profile.Name);
                if (securePassword.Length == 0)
                {
                    return new ConnectionResult(false, errorMessage: "No saved credentials found");
                }

                // Create credential without using statement
                var credential = new PSCredential(profile.Username, securePassword);
                return await _powerShellManager.TestVCenterConnectionAsync(profile, credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed for {Server}", profile.ServerAddress);
                return new ConnectionResult(false, errorMessage: ex.Message);
            }
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
                _credentialManager.DeletePassword(profileName); // Clean up credentials too
                LoadProfiles();
                _logger.LogInformation("Removed profile: {ProfileName}", profileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove profile");
                throw;
            }
        }


        // ... [rest of your existing ConnectionManager methods] ...
    }
}
