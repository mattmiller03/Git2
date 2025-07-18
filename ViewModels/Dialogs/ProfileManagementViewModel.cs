using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using Wpf.Ui.Controls;
using System.Windows;

namespace UiDesktopApp2.ViewModels.Dialogs
{
    public partial class ProfileManagementViewModel : ObservableObject
    {
        private readonly IProfileManager _profileManager;
        private readonly ICredentialManager _credentialManager;
        private readonly ConnectionManager _connectionManager;
        private readonly ILogger<ProfileManagementViewModel> _logger;

        public event Action<ConnectionProfile>? ProfileSelected;

        [ObservableProperty]
        private ObservableCollection<ConnectionProfile> _profiles = new();

        [ObservableProperty]
        private ICollectionView _filteredProfiles;

        [ObservableProperty]
        private ConnectionProfile? _selectedProfile;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _statusTitle = "Ready";

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

        [ObservableProperty]
        private bool _showStatus = false;

        public ProfileManagementViewModel(
            IProfileManager profileManager,
            ICredentialManager credentialManager,
            ConnectionManager connectionManager,
            ILogger<ProfileManagementViewModel> logger)
        {
            _profileManager = profileManager;
            _credentialManager = credentialManager;
            _connectionManager = connectionManager;
            _logger = logger;

            // Initialize filtered collection
            FilteredProfiles = CollectionViewSource.GetDefaultView(Profiles);
            FilteredProfiles.Filter = FilterProfiles;

            // Subscribe to search text changes
            PropertyChanged += OnPropertyChanged;

            // Load profiles
            _ = Task.Run(LoadProfilesAsync);
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
            {
                FilteredProfiles.Refresh();
            }
        }

        private bool FilterProfiles(object item)
        {
            if (item is ConnectionProfile profile && !string.IsNullOrWhiteSpace(SearchText))
            {
                return profile.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       profile.ServerAddress.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       profile.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }

        [RelayCommand]
        private async Task LoadProfilesAsync()
        {
            try
            {
                // Run the profile loading on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Profiles.Clear();
                    var profiles = _profileManager.GetAllProfiles();

                    foreach (var profile in profiles)
                    {
                        Profiles.Add(profile);
                    }
                });

                ShowStatusMessage("Profiles Loaded", $"Loaded {Profiles.Count} profiles", InfoBarSeverity.Success);
                _logger.LogInformation("Loaded {Count} profiles", Profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profiles");
                ShowStatusMessage("Load Error", "Failed to load profiles", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadProfilesAsync();
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if (SelectedProfile == null) return;

            try
            {
                ShowStatusMessage("Testing Connection", $"Testing connection to {SelectedProfile.ServerAddress}...", InfoBarSeverity.Informational);

                var result = await _connectionManager.TestConnectionAsync(SelectedProfile);

                if (result.IsSuccessful)
                {
                    ShowStatusMessage("Connection Successful", $"Successfully connected to {SelectedProfile.ServerAddress}", InfoBarSeverity.Success);
                }
                else
                {
                    ShowStatusMessage("Connection Failed", result.ErrorMessage ?? "Unknown error", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                ShowStatusMessage("Test Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void EditProfile()
        {
            if (SelectedProfile == null) return;
            ShowStatusMessage("Edit Profile", "Edit profile functionality coming soon", InfoBarSeverity.Informational);
        }

        [RelayCommand]
        private void DuplicateProfile()
        {
            if (SelectedProfile == null) return;

            try
            {
                var duplicateName = $"{SelectedProfile.Name} - Copy";
                var counter = 1;

                while (_profileManager.GetProfile(duplicateName) != null)
                {
                    duplicateName = $"{SelectedProfile.Name} - Copy ({counter})";
                    counter++;
                }

                var duplicateProfile = new ConnectionProfile
                {
                    Name = duplicateName,
                    ServerAddress = SelectedProfile.ServerAddress,
                    Username = SelectedProfile.Username,
                    Password = SelectedProfile.Password
                };

                _profileManager.SaveProfile(duplicateProfile);

                // Copy credentials if they exist
                var securePassword = _credentialManager.GetPassword(SelectedProfile.Name);
                if (securePassword.Length > 0)
                {
                    _credentialManager.SavePassword(duplicateProfile.Name, duplicateProfile.Username, securePassword);
                }

                _ = Task.Run(LoadProfilesAsync);
                ShowStatusMessage("Profile Duplicated", $"Created duplicate profile: {duplicateName}", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to duplicate profile");
                ShowStatusMessage("Duplicate Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void ExportProfile()
        {
            if (SelectedProfile == null) return;
            ShowStatusMessage("Export Profile", "Export functionality coming soon", InfoBarSeverity.Informational);
        }

        [RelayCommand]
        private void DeleteProfile()
        {
            if (SelectedProfile == null) return;

            try
            {
                var profileName = SelectedProfile.Name;

                _profileManager.DeleteProfile(profileName);

                // Delete credentials
                try
                {
                    _credentialManager.DeletePassword(profileName);
                }
                catch (Exception credEx)
                {
                    _logger.LogWarning(credEx, "Failed to delete credentials for profile {ProfileName}", profileName);
                }

                _ = Task.Run(LoadProfilesAsync);
                SelectedProfile = null;

                ShowStatusMessage("Profile Deleted", $"Deleted profile: {profileName}", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete profile");
                ShowStatusMessage("Delete Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void AddNewProfile()
        {
            ShowStatusMessage("Add Profile", "Add new profile functionality coming soon", InfoBarSeverity.Informational);
        }

        [RelayCommand]
        private void ImportProfiles()
        {
            ShowStatusMessage("Import Profiles", "Import functionality coming soon", InfoBarSeverity.Informational);
        }

        [RelayCommand]
        private void SelectProfile()
        {
            if (SelectedProfile != null)
            {
                ProfileSelected?.Invoke(SelectedProfile);
            }
        }

        public void ShowStatusMessage(string title, string message, InfoBarSeverity severity)
        {
            StatusTitle = title;
            StatusMessage = message;
            StatusSeverity = severity;
            ShowStatus = true;

            // Auto-hide after 3 seconds for non-error messages
            if (severity != InfoBarSeverity.Error)
            {
                _ = Task.Delay(3000).ContinueWith(_ => ShowStatus = false);
            }
        }
    }
}
