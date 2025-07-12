using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using CommunityToolkit.Mvvm.Input;
{
    
}

namespace UiDesktopApp2.ViewModels.Pages
{
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        // Initialize commands in the constructor to ensure non-null values
        public RelayCommand NewProfileCommand { get; }
        public RelayCommand SaveProfileCommand { get; }
        public RelayCommand DeleteProfileCommand { get; }

        // Constructor
        public ConnectionViewModel()
        {
            NewProfileCommand = new RelayCommand(OnNewProfile);
            SaveProfileCommand = new RelayCommand(OnSaveProfile);
            DeleteProfileCommand = new RelayCommand(OnDeleteProfile);
        }

        // Implement the PropertyChanged event required by INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        // Helper method to raise the PropertyChanged event
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Command methods
        private void OnNewProfile()
        {
            // Logic for creating a new profile
        }

        private void OnSaveProfile()
        {
            // Logic for saving a profile
        }

        private void OnDeleteProfile()
        {
            // Logic for deleting a profile
        }
    }
}
