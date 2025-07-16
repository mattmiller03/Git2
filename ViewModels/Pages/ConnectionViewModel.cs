using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Security;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class ConnectionViewModel : ObservableObject
    {
        private readonly ConnectionManager _connectionManager;
        private readonly IProfileManager _profileManager;
        private readonly ICredentialManager _credentialManager;

        public ConnectionViewModel(ConnectionManager connectionManager, IProfileManager profileManager, ICredentialManager credentialManager, ILogger<ConnectionViewModel> logger)
        {
            _connectionManager = connectionManager;
            _profileManager = profileManager;
            _credentialManager = credentialManager;

            LoadSavedProfiles();
        }

        // Source vCenter properties
        [ObservableProperty]
        private string sourceServerAddress = string.Empty;

        [ObservableProperty]
        private string sourceUsername = string.Empty;

        [ObservableProperty]
        private string sourcePassword = string.Empty;

        // Destination vCenter properties
        [ObservableProperty]
        private string destinationServerAddress = string.Empty;

        [ObservableProperty]
        private string destinationUsername = string.Empty;

        [ObservableProperty]
        private string destinationPassword = string.Empty;

        // Saved profiles collection
        [ObservableProperty]
        private ObservableCollection<ConnectionProfile> savedProfiles = new();

        // Selected profile from ComboBox - FIXED: Made nullable to resolve CS8618 warning
        [ObservableProperty]
        private ConnectionProfile? selectedProfile;

        // Status info properties
        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private bool isStatusOpen = false;

        private void LoadSavedProfiles()
        {
            SavedProfiles.Clear();
            var profiles = _profileManager.GetAllProfiles();
            foreach (var profile in profiles)
            {
                SavedProfiles.Add(profile);
            }
        }

        [RelayCommand]
        private async Task TestSourceConnection()
        {
            StatusMessage = "Testing source connection...";
            IsStatusOpen = true;

            var profile = new ConnectionProfile
            {
                ServerAddress = SourceServerAddress,
                Username = SourceUsername,
                Password = SourcePassword
            };

            var result = await _connectionManager.TestConnectionAsync(profile);
            StatusMessage = result.IsSuccessful ? "Source connection successful" : $"Source connection failed: {result.ErrorMessage}";
        }

        [RelayCommand]
        private void SaveSourceProfile()
        {
            var profile = new ConnectionProfile
            {
                ServerAddress = SourceServerAddress,
                Username = SourceUsername,
                Password = SourcePassword,
                Name = $"Source-{SourceServerAddress}"
            };

            _profileManager.SaveProfile(profile);
            LoadSavedProfiles();
            StatusMessage = "Source profile saved";
            IsStatusOpen = true;
        }

        [RelayCommand]
        private async Task TestDestinationConnection()
        {
            StatusMessage = "Testing destination connection...";
            IsStatusOpen = true;

            var profile = new ConnectionProfile
            {
                ServerAddress = DestinationServerAddress,
                Username = DestinationUsername,
                Password = DestinationPassword
            };

            var result = await _connectionManager.TestConnectionAsync(profile);
            StatusMessage = result.IsSuccessful ? "Destination connection successful" : $"Destination connection failed: {result.ErrorMessage}";
        }

        [RelayCommand]
        private void SaveDestinationProfile()
        {
            var profile = new ConnectionProfile
            {
                ServerAddress = DestinationServerAddress,
                Username = DestinationUsername,
                Password = DestinationPassword,
                Name = $"Destination-{DestinationServerAddress}"
            };

            _profileManager.SaveProfile(profile);
            LoadSavedProfiles();
            StatusMessage = "Destination profile saved";
            IsStatusOpen = true;
        }

        [RelayCommand]
        private void LoadProfile()
        {
            if (SelectedProfile == null)
                return;

            // Load selected profile into both source and destination fields
            SourceServerAddress = SelectedProfile.ServerAddress;
            SourceUsername = SelectedProfile.Username;
            SourcePassword = SelectedProfile.Password;

            DestinationServerAddress = SelectedProfile.ServerAddress;
            DestinationUsername = SelectedProfile.Username;
            DestinationPassword = SelectedProfile.Password;

            StatusMessage = $"Loaded profile: {SelectedProfile.Name}";
            IsStatusOpen = true;
        }

        [RelayCommand]
        private void DeleteProfile()
        {
            if (SelectedProfile == null)
                return;

            _profileManager.DeleteProfile(SelectedProfile.Name);
            LoadSavedProfiles();

            StatusMessage = $"Deleted profile: {SelectedProfile.Name}";
            IsStatusOpen = true;
        }
    }
}
