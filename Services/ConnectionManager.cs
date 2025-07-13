using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;

namespace UiDesktopApp2.Services
{
    public class ConnectionManager
    {
        public ObservableCollection<ConnectionProfile> ServerProfiles { get; set; } = new();

        public ConnectionProfile? SelectedSourceProfile { get; set; }

        public ConnectionProfile? SelectedDestinationProfile { get; set; }

        public ICommand OpenProfileManagerCommand { get; }

        public ICommand TestSourceConnectionCommand { get; }

        private readonly IProfileManager _profileManager;

        public ConnectionManager(IProfileManager profileManager)
        {
            _profileManager = profileManager;

            // Initialize profiles from profile manager
            LoadProfiles();

            // Initialize commands
            OpenProfileManagerCommand = new RelayCommand(OpenProfileManager);
            TestSourceConnectionCommand = new RelayCommand(TestSourceConnection);
        }

        private void LoadProfiles()
        {
            var profiles = _profileManager.GetAllProfiles();
            ServerProfiles.Clear();
            foreach (var profile in profiles)
            {
                ServerProfiles.Add(profile);
            }
        }

        private void OpenProfileManager()
        {
            // Implement profile manager opening logic
            // This could be a dialog or a new window
        }

        private void TestSourceConnection()
        {
            if (SelectedSourceProfile == null)
            {
                // Handle no profile selected
                return;
            }

            // Implement connection testing logic
            // You might want to use PowerShell or another connection method
        }

        // Additional methods for managing profiles
        public void AddProfile(ConnectionProfile profile)
        {
            _profileManager.SaveProfile(profile);
            LoadProfiles();
        }

        public void RemoveProfile(string profileName)
        {
            _profileManager.DeleteProfile(profileName);
            LoadProfiles();
        }
    }
}
