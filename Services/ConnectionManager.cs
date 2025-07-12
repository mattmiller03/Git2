using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;


namespace UiDesktopApp2.Services
{
    public class ConnectionManager
    {
        private readonly IProfileManager _profileManager;
        private readonly PowerShellManager _powerShellManager;

        // Constructor to initialize non-nullable fields  
        public ConnectionManager(IProfileManager profileManager, PowerShellManager powerShellManager)
        {
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _powerShellManager = powerShellManager ?? throw new ArgumentNullException(nameof(powerShellManager));
        }

        // Your existing properties and methods...  
        public ObservableCollection<ConnectionProfile> ServerProfiles { get; }
        public ConnectionProfile SelectedSourceProfile { get; set; }
        public ConnectionProfile SelectedDestinationProfile { get; set; }

        public ICommand OpenProfileManagerCommand { get; }
        public ICommand TestSourceConnectionCommand { get; }

        // Example usage of _profileManager to avoid CS0169  
        public IEnumerable<ConnectionProfile> GetAllProfiles()
        {
            return _profileManager.GetAllProfiles();
        }
    }
}
