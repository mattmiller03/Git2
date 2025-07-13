using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using Wpf.Ui.Controls;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ConnectionManager _connectionManager;
        private readonly IProfileManager _profileManager;
        private readonly PowerShellManager _powerShellManager;

        [ObservableProperty]
        private ObservableCollection<ConnectionProfile> _serverProfiles = new();

        [ObservableProperty]
        private ConnectionProfile? _selectedProfile;

        [ObservableProperty]
        private int _totalProfiles;

        [ObservableProperty]
        private int _connectedProfiles;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

        public DashboardViewModel(
            ConnectionManager connectionManager,
            IProfileManager profileManager,
            PowerShellManager powerShellManager)
        {
            _connectionManager = connectionManager;
            _profileManager = profileManager;
            _powerShellManager = powerShellManager;

            LoadProfiles();
        }

        private void LoadProfiles()
        {
            try
            {
                IsLoading = true;
                ServerProfiles.Clear();

                var profiles = _profileManager.GetAllProfiles();

                foreach (var profile in profiles)
                {
                    ServerProfiles.Add(profile);
                }

                TotalProfiles = ServerProfiles.Count;
                UpdateConnectionStatus();

                StatusMessage = $"Loaded {TotalProfiles} profiles";
                StatusSeverity = InfoBarSeverity.Success;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading profiles: {ex.Message}";
                StatusSeverity = InfoBarSeverity.Error;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void UpdateConnectionStatus()
        {
            ConnectedProfiles = 0;
            foreach (var profile in ServerProfiles)
            {
                try
                {
                    var connectionResult = await TestProfileConnectionAsync(profile);
                    if (connectionResult.IsSuccessful)
                    {
                        ConnectedProfiles++;
                    }
                }
                catch
                {
                    // Silently handle connection test failures
                }
            }
        }

        [RelayCommand]
        private void RefreshProfiles()
        {
            LoadProfiles();
        }

        [RelayCommand]
        private void AddNewProfile()
        {
            // TODO: Implement profile creation logic
            // This could open a dialog or navigate to a profile creation page
            StatusMessage = "Add new profile functionality not implemented";
            StatusSeverity = InfoBarSeverity.Warning;
        }

        [RelayCommand]
        private void EditProfile(ConnectionProfile? profile)
        {
            if (profile == null) return;

            // TODO: Implement profile editing logic
            StatusMessage = $"Editing profile: {profile.Name}";
            StatusSeverity = InfoBarSeverity.Informational;
        }

        [RelayCommand]
        private void DeleteProfile(ConnectionProfile? profile)
        {
            if (profile == null) return;

            try
            {
                _profileManager.DeleteProfile(profile.Name);
                LoadProfiles();

                StatusMessage = $"Profile {profile.Name} deleted successfully";
                StatusSeverity = InfoBarSeverity.Success;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting profile: {ex.Message}";
                StatusSeverity = InfoBarSeverity.Error;
            }
        }

        [RelayCommand]
        private async Task TestConnectionAsync(ConnectionProfile? profile)
        {
            if (profile == null) return;

            try
            {
                IsLoading = true;
                var result = await TestProfileConnectionAsync(profile);

                if (result.IsSuccessful)
                {
                    StatusMessage = $"Successfully connected to {profile.Name}";
                    StatusSeverity = InfoBarSeverity.Success;
                }
                else
                {
                    StatusMessage = $"Connection failed: {result.ErrorMessage}";
                    StatusSeverity = InfoBarSeverity.Error;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unexpected error: {ex.Message}";
                StatusSeverity = InfoBarSeverity.Error;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<ConnectionResult> TestProfileConnectionAsync(ConnectionProfile profile)
        {
            // Implement actual connection testing logic
            // This is a placeholder - replace with actual PowerShell or vSphere SDK connection test
            try
            {
                await Task.Delay(500); // Simulate connection test
                return new ConnectionResult(
                    isSuccessful: true,
                    version: "1.0"
                );
            }
            catch (Exception ex)
            {
                return new ConnectionResult(
                    isSuccessful: false,
                    errorMessage: ex.Message
                );
            }
        }

        [ObservableProperty]
        private ObservableCollection<RecentMigration> _recentMigrations = new();

        private void LoadRecentMigrations()
        {
            // TODO: Implement loading recent migrations from a service or database
            RecentMigrations.Clear();
            // Example mock data
            RecentMigrations.Add(new RecentMigration
            {
                VmName = "TestVM1",
                SourceCluster = "Cluster1",
                DestinationCluster = "Cluster2",
                MigrationDate = DateTime.Now.AddDays(-1)
            });
        }
    }
}
