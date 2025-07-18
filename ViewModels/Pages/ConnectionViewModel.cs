using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Security;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using UiDesktopApp2.ViewModels.Dialogs;
using UiDesktopApp2.Views.Dialogs;
using Wpf.Ui;
using Wpf.Ui.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class ConnectionViewModel : ObservableObject
    {
        private readonly ConnectionManager _connectionManager;
        private readonly IProfileManager _profileManager;
        private readonly ICredentialManager _credentialManager;
        private readonly ILogger<ConnectionViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IContentDialogService _contentDialogService;

        // Update constructor
        public ConnectionViewModel(
            ConnectionManager connectionManager,
            IProfileManager profileManager,
            ICredentialManager credentialManager,
            ILogger<ConnectionViewModel> logger,
            IServiceProvider serviceProvider,
            IContentDialogService contentDialogService)
        {
            _connectionManager = connectionManager;
            _profileManager = profileManager;
            _credentialManager = credentialManager;
            _logger = logger;
            _serviceProvider = serviceProvider;

            InitializeAvailableServers();
        }

        [RelayCommand]
        private async Task ManageProfilesAsync()
        {
            try
            {
                var dialogViewModel = _serviceProvider.GetRequiredService<ProfileManagementViewModel>();
                var dialog = new Views.Dialogs.ProfileManagementDialog(dialogViewModel);

                // Show the dialog directly (no Owner needed)
                var result = await dialog.ShowAsync();

                // Check if a profile was selected
                var selectedProfile = dialog.SelectedProfileResult ?? dialog.GetSelectedProfile();

                if (selectedProfile != null)
                {
                    // Load the selected profile
                    var matchingServer = AvailableServers.FirstOrDefault(s => s.Address == selectedProfile.ServerAddress);

                    if (matchingServer != null)
                    {
                        SelectedSourceServer = matchingServer;
                    }
                    else
                    {
                        // Set as custom server
                        var customServer = AvailableServers.FirstOrDefault(s => s.Name == "Custom Server");
                        if (customServer != null)
                        {
                            customServer.Address = selectedProfile.ServerAddress;
                            SelectedSourceServer = customServer;
                        }
                    }

                    SourceUsername = selectedProfile.Username;

                    // Load password from credential manager
                    var securePassword = _credentialManager.GetPassword(selectedProfile.Name);
                    if (securePassword.Length > 0)
                    {
                        var password = new System.Net.NetworkCredential(string.Empty, securePassword).Password;
                        SourcePassword = password;
                    }

                    StatusMessage = $"Loaded profile: {selectedProfile.Name}";
                    IsStatusOpen = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open profile management dialog");
                StatusMessage = $"Failed to open profile management: {ex.Message}";
                IsStatusOpen = true;
            }
        }


        // Available vCenter servers for dropdown selection
        [ObservableProperty]
        private ObservableCollection<VCenterServer> _availableServers = new();

        // Source vCenter properties
        [ObservableProperty]
        private VCenterServer? _selectedSourceServer;

        [ObservableProperty]
        private string _sourceUsername = string.Empty;

        [ObservableProperty]
        private string _sourcePassword = string.Empty;

        // Destination vCenter properties
        [ObservableProperty]
        private VCenterServer? _selectedDestinationServer;

        [ObservableProperty]
        private string _destinationUsername = string.Empty;

        [ObservableProperty]
        private string _destinationPassword = string.Empty;

        // Profile management
        [ObservableProperty]
        private string _profileName = string.Empty;

        // Saved profiles collection
        [ObservableProperty]
        private ObservableCollection<ConnectionProfile> _savedProfiles = new();

        // Selected profile from saved profiles list
        [ObservableProperty]
        private ConnectionProfile? _selectedProfile;

        // Status info properties
        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isStatusOpen = false;

        private void InitializeAvailableServers()
        {
            // Add your predefined vCenter servers here
            // In a real application, these might come from a configuration file or database
            AvailableServers.Add(new VCenterServer
            {
                Name = "Production vCenter",
                Address = "vcenter-prod.domain.com",
                Description = "Production Environment"
            });

            AvailableServers.Add(new VCenterServer
            {
                Name = "Development vCenter",
                Address = "vcenter-dev.domain.com",
                Description = "Development Environment"
            });

            AvailableServers.Add(new VCenterServer
            {
                Name = "Test vCenter",
                Address = "vcenter-test.domain.com",
                Description = "Test Environment"
            });

            AvailableServers.Add(new VCenterServer
            {
                Name = "DR vCenter",
                Address = "vcenter-dr.domain.com",
                Description = "Disaster Recovery Site"
            });

            // Add option for custom server
            AvailableServers.Add(new VCenterServer
            {
                Name = "Custom Server",
                Address = "",
                Description = "Enter custom server address"
            });
        }

        private void LoadSavedProfiles()
        {
            try
            {
                SavedProfiles.Clear();
                var profiles = _profileManager.GetAllProfiles();

                _logger.LogInformation($"Loading {profiles.Count()} profiles from profile manager");

                foreach (var profile in profiles)
                {
                    SavedProfiles.Add(profile);
                    _logger.LogDebug($"Loaded profile: '{profile.Name}' - {profile.ServerAddress}");
                }

                _logger.LogInformation("Successfully loaded {Count} saved profiles", SavedProfiles.Count);

                StatusMessage = $"Loaded {SavedProfiles.Count} saved profiles";
                IsStatusOpen = true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to load saved profiles");
                StatusMessage = "Failed to load saved profiles";
                IsStatusOpen = true;
            }
        }

        [RelayCommand]
        private async Task TestSourceConnection()
        {
            try
            {
                if (SelectedSourceServer == null)
                {
                    StatusMessage = "Please select a source server";
                    IsStatusOpen = true;
                    return;
                }

                StatusMessage = "Testing source connection...";
                IsStatusOpen = true;

                var serverAddress = SelectedSourceServer.Name == "Custom Server"
                    ? SelectedSourceServer.Address
                    : SelectedSourceServer.Address;

                var profile = new ConnectionProfile
                {
                    ServerAddress = serverAddress,
                    Username = SourceUsername,
                    Password = SourcePassword
                };

                var result = await _connectionManager.TestConnectionAsync(profile);
                StatusMessage = result.IsSuccessful ? "Source connection successful" : $"Source connection failed: {result.ErrorMessage}";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Source connection test failed");
                StatusMessage = $"Connection test error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task TestDestinationConnection()
        {
            try
            {
                if (SelectedDestinationServer == null)
                {
                    StatusMessage = "Please select a destination server";
                    IsStatusOpen = true;
                    return;
                }

                StatusMessage = "Testing destination connection...";
                IsStatusOpen = true;

                var serverAddress = SelectedDestinationServer.Name == "Custom Server"
                    ? SelectedDestinationServer.Address
                    : SelectedDestinationServer.Address;

                var profile = new ConnectionProfile
                {
                    ServerAddress = serverAddress,
                    Username = DestinationUsername,
                    Password = DestinationPassword
                };

                var result = await _connectionManager.TestConnectionAsync(profile);
                StatusMessage = result.IsSuccessful ? "Destination connection successful" : $"Destination connection failed: {result.ErrorMessage}";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Destination connection test failed");
                StatusMessage = $"Connection test error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SaveAsProfile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    StatusMessage = "Please enter a profile name";
                    IsStatusOpen = true;
                    return;
                }

                if (SelectedSourceServer == null)
                {
                    StatusMessage = "Please select a source server";
                    IsStatusOpen = true;
                    return;
                }

                var serverAddress = SelectedSourceServer.Name == "Custom Server"
                    ? SelectedSourceServer.Address
                    : SelectedSourceServer.Address;

                if (string.IsNullOrWhiteSpace(serverAddress))
                {
                    StatusMessage = "Please enter a server address";
                    IsStatusOpen = true;
                    return;
                }

                // Check for existing profile with same name
                var existingProfile = _profileManager.GetProfile(ProfileName);
                if (existingProfile != null)
                {
                    StatusMessage = $"Profile '{ProfileName}' already exists. Please choose a different name.";
                    IsStatusOpen = true;
                    return;
                }

                var profile = new ConnectionProfile
                {
                    Name = ProfileName,
                    ServerAddress = serverAddress,
                    Username = SourceUsername,
                    Password = SourcePassword
                };

                _profileManager.SaveProfile(profile);

                // Save credentials securely
                if (!string.IsNullOrWhiteSpace(SourcePassword))
                {
                    var securePassword = new System.Security.SecureString();
                    foreach (char c in SourcePassword)
                    {
                        securePassword.AppendChar(c);
                    }
                    securePassword.MakeReadOnly();

                    _credentialManager.SavePassword(profile.Name, profile.Username, securePassword);
                }

                LoadSavedProfiles();

                // Clear the profile name field
                ProfileName = string.Empty;

                StatusMessage = $"Profile '{profile.Name}' saved successfully";
                IsStatusOpen = true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to save profile");
                StatusMessage = $"Failed to save profile: {ex.Message}";
                IsStatusOpen = true;
            }
        }

        [RelayCommand]
        private void LoadProfile()
        {
            try
            {
                if (SelectedProfile == null)
                {
                    StatusMessage = "Please select a profile to load";
                    IsStatusOpen = true;
                    return;
                }

                // Find the server in available servers or set as custom
                var matchingServer = AvailableServers.FirstOrDefault(s => s.Address == SelectedProfile.ServerAddress);

                if (matchingServer != null)
                {
                    SelectedSourceServer = matchingServer;
                }
                else
                {
                    // Set as custom server
                    var customServer = AvailableServers.FirstOrDefault(s => s.Name == "Custom Server");
                    if (customServer != null)
                    {
                        customServer.Address = SelectedProfile.ServerAddress;
                        SelectedSourceServer = customServer;
                    }
                }

                SourceUsername = SelectedProfile.Username;

                // Load password from credential manager
                var securePassword = _credentialManager.GetPassword(SelectedProfile.Name);
                if (securePassword.Length > 0)
                {
                    var password = new System.Net.NetworkCredential(string.Empty, securePassword).Password;
                    SourcePassword = password;
                }

                StatusMessage = $"Loaded profile: {SelectedProfile.Name}";
                IsStatusOpen = true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to load profile");
                StatusMessage = $"Failed to load profile: {ex.Message}";
                IsStatusOpen = true;
            }
        }

        [RelayCommand]
        private void DeleteProfile()
        {
            try
            {
                if (SelectedProfile == null)
                {
                    StatusMessage = "Please select a profile to delete";
                    IsStatusOpen = true;
                    return;
                }

                var profileName = SelectedProfile.Name;

                _profileManager.DeleteProfile(profileName);

                // Delete credentials
                try
                {
                    _credentialManager.DeletePassword(profileName);
                }
                catch (System.Exception credEx)
                {
                    _logger.LogWarning(credEx, $"Failed to delete credentials for profile '{profileName}'");
                }

                LoadSavedProfiles();
                SelectedProfile = null;

                StatusMessage = $"Successfully deleted profile: {profileName}";
                IsStatusOpen = true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to delete profile");
                StatusMessage = $"Failed to delete profile: {ex.Message}";
                IsStatusOpen = true;
            }
        }

        public IEnumerable<ConnectionProfile> GetAllProfiles()
        {
            return _profiles.ToList(); // Return a copy to prevent external modification
        }
        [RelayCommand]
        private void DebugProfiles()
        {
            try
            {
                _logger.LogInformation("=== DEBUG: Profile Information ===");

                // Check UI collection
                _logger.LogInformation($"UI Collection has {SavedProfiles.Count} profiles:");
                foreach (var profile in SavedProfiles)
                {
                    _logger.LogInformation($"  UI Profile: '{profile.Name}' - {profile.ServerAddress}");
                }

                // Check profile manager
                var allProfiles = _profileManager.GetAllProfiles().ToList();
                _logger.LogInformation($"Profile Manager has {allProfiles.Count} profiles:");
                foreach (var profile in allProfiles)
                {
                    _logger.LogInformation($"  Storage Profile: '{profile.Name}' - {profile.ServerAddress}");
                }

                // Check selected profile
                if (SelectedProfile != null)
                {
                    _logger.LogInformation($"Selected Profile: '{SelectedProfile.Name}' - {SelectedProfile.ServerAddress}");

                    // Check if it exists in storage
                    var existsInStorage = _profileManager.GetProfile(SelectedProfile.Name);
                    _logger.LogInformation($"Selected profile exists in storage: {existsInStorage != null}");
                }
                else
                {
                    _logger.LogInformation("No profile selected");
                }

                StatusMessage = "Debug info logged - check console/logs";
                IsStatusOpen = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Debug profiles failed");
                StatusMessage = $"Debug failed: {ex.Message}";
                IsStatusOpen = true;
            }
        }
        [RelayCommand]
        private void RefreshProfiles()
        {
            LoadSavedProfiles();
        }

        [RelayCommand]
        private void CopySourceToDestination()
        {
            SelectedDestinationServer = SelectedSourceServer;
            DestinationUsername = SourceUsername;
            DestinationPassword = SourcePassword;

            StatusMessage = "Copied source connection to destination";
            IsStatusOpen = true;
        }
    }
}
