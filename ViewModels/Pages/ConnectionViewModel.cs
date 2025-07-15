using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class ConnectionViewModel(
        ConnectionManager connectionManager,
        ICredentialManager credentialManager,
        ILogger<ConnectionViewModel> logger) : ObservableObject
    {
        private readonly ConnectionManager _connectionManager = connectionManager;
        private readonly ICredentialManager _credentialManager = credentialManager;
        private readonly ILogger<ConnectionViewModel> _logger = logger;

        [ObservableProperty]
        private ConnectionProfile _currentProfile = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
        private string _statusMessage = string.Empty;

        public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

        [RelayCommand]
        private async Task OnSaveProfile()
        {
            if (CurrentProfile == null) return;

            try
            {
                // Convert password to SecureString
                var securePassword = new SecureString();
                foreach (char c in CurrentProfile.Password)
                {
                    securePassword.AppendChar(c);
                }
                securePassword.MakeReadOnly();

                // Save credentials asynchronously
                await Task.Run(() =>
                {
                    _credentialManager.SavePassword(
                        CurrentProfile.Name,
                        CurrentProfile.Username,
                        securePassword);

                    // Save profile (without password)
                    CurrentProfile.Password = string.Empty;
                    _connectionManager.AddProfile(CurrentProfile);
                });

                StatusMessage = "Profile saved successfully";
                _logger.LogInformation("Saved profile: {ProfileName}", CurrentProfile.Name);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving profile: {ex.Message}";
                _logger.LogError(ex, "Failed to save profile");
            }
        }

        [RelayCommand]
        private async Task OnTestConnection()
        {
            if (CurrentProfile == null)
            {
                StatusMessage = "No profile selected";
                return;
            }

            try
            {
                var result = await _connectionManager.TestConnectionAsync(CurrentProfile);
                StatusMessage = result.IsSuccessful
                    ? $"Connection successful! Version: {result.Version}"
                    : $"Connection failed: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error testing connection: {ex.Message}";
                _logger.LogError(ex, "Connection test failed");
            }
        }

        [RelayCommand]
        private void ClearStatus()
        {
            StatusMessage = string.Empty;
        }
    }
}