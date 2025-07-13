using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using Wpf.Ui.Controls;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class ValidationViewModel : ObservableObject
    {
        private readonly IProfileManager _profileManager;
        private readonly ConnectionManager _connectionManager;
        private readonly PowerShellManager _powerShellManager;

        [ObservableProperty]
        private ObservableCollection<ConnectionProfile> _connectionProfiles = new();

        [ObservableProperty]
        private ObservableCollection<ValidationResult> _validationResults = new();

        [ObservableProperty]
        private ConnectionProfile? _selectedProfile;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusTitle = "Ready";

        [ObservableProperty]
        private string _statusMessage = "Connection validation system initialized";

        [ObservableProperty]
        private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

        [ObservableProperty]
        private bool _showStatus;

        public ValidationViewModel(
            IProfileManager profileManager,
            ConnectionManager connectionManager,
            PowerShellManager powerShellManager)
        {
            _profileManager = profileManager;
            _connectionManager = connectionManager;
            _powerShellManager = powerShellManager;

            LoadProfiles();
        }

        private void LoadProfiles()
        {
            ConnectionProfiles.Clear();
            var profiles = _profileManager.GetAllProfiles();

            foreach (var profile in profiles)
            {
                ConnectionProfiles.Add(profile);
            }
        }

        [RelayCommand]
        private void RefreshProfiles()
        {
            LoadProfiles();
            ValidationResults.Clear();
        }

        [RelayCommand]
        private void AddNewProfile()
        {
            // TODO: Implement profile creation logic
            StatusTitle = "Add Profile";
            StatusMessage = "Profile creation not yet implemented";
            StatusSeverity = InfoBarSeverity.Warning;
            ShowStatus = true;
        }

        [RelayCommand]
        private async Task TestConnectionAsync(ConnectionProfile? profile)
        {
            if (profile == null) return;

            try
            {
                IsLoading = true;
                var result = await ValidateProfileConnectionAsync(profile);

                ValidationResults.Insert(0, result);

                StatusTitle = result.Status;
                StatusMessage = result.Message;
                StatusSeverity = result.Status == "Success"
                    ? InfoBarSeverity.Success
                    : InfoBarSeverity.Error;
                ShowStatus = true;
            }
            catch (Exception ex)
            {
                StatusTitle = "Error";
                StatusMessage = $"Unexpected error: {ex.Message}";
                StatusSeverity = InfoBarSeverity.Error;
                ShowStatus = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void EditProfile(ConnectionProfile? profile)
        {
            if (profile == null) return;

            // TODO: Implement profile editing logic
            StatusTitle = "Edit Profile";
            StatusMessage = $"Editing profile: {profile.Name}";
            StatusSeverity = InfoBarSeverity.Informational;
            ShowStatus = true;
        }

        [RelayCommand]
        private void DeleteProfile(ConnectionProfile? profile)
        {
            if (profile == null) return;

            try
            {
                _profileManager.DeleteProfile(profile.Name);
                LoadProfiles();

                StatusTitle = "Profile Deleted";
                StatusMessage = $"Profile {profile.Name} deleted successfully";
                StatusSeverity = InfoBarSeverity.Success;
                ShowStatus = true;
            }
            catch (Exception ex)
            {
                StatusTitle = "Delete Error";
                StatusMessage = $"Error deleting profile: {ex.Message}";
                StatusSeverity = InfoBarSeverity.Error;
                ShowStatus = true;
            }
        }

        private async Task<ValidationResult> ValidateProfileConnectionAsync(ConnectionProfile profile)
        {
            try
            {
                var connectionResult = await _connectionManager.TestConnectionAsync(profile);

                return new ValidationResult
                {
                    ProfileName = profile.Name,
                    Status = connectionResult.IsSuccessful ? "Success" : "Failed",
                    Version = connectionResult.Version ?? "N/A",
                    Message = connectionResult.IsSuccessful
                        ? "Connection established successfully"
                        : connectionResult.ErrorMessage ?? "Unknown error"
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    ProfileName = profile.Name,
                    Status = "Error",
                    Version = "N/A",
                    Message = ex.Message
                };
            }
        }
    }

    // Supporting model for validation results
    public class ValidationResult
    {
        public string ProfileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}


