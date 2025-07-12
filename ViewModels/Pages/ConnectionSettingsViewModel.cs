using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.ViewModels.Pages
{
    public class ConnectionSettingsViewModel : INotifyPropertyChanged
    {
        private readonly IProfileManager _profileManager;
        private readonly ICredentialManager _credentialManager;

        public ConnectionSettingsViewModel(
            IProfileManager profileManager,
            ICredentialManager credentialManager)
        {
            _profileManager = profileManager;
            _credentialManager = credentialManager;

            NewProfileCommand = new RelayCommand(NewProfile);
            SaveProfileCommand = new RelayCommand(SaveProfile, CanSaveProfile);
            DeleteProfileCommand = new RelayCommand(DeleteProfile, CanDeleteProfile);

            LoadProfiles();
        }

        #region Backing fields & properties

        private string _searchFilter;
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                    OnPropertyChanged(nameof(FilteredProfiles));
            }
        }

        public ObservableCollection<ConnectionProfile> Profiles { get; }
            = new ObservableCollection<ConnectionProfile>();

        public IEnumerable<ConnectionProfile> FilteredProfiles =>
            string.IsNullOrWhiteSpace(_searchFilter)
                ? Profiles
                : Profiles.Where(p =>
                    p.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    p.SourceVCenter.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    p.SourceUsername.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)
                  );

        private ConnectionProfile _selectedProfile;
        public ConnectionProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value))
                {
                    // load/delete the saved password
                    Password = value != null
                        ? _credentialManager.GetPassword(value.Name)
                        : new SecureString();

                    SaveProfileCommand.RaiseCanExecuteChanged();
                    DeleteProfileCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private SecureString _password = new SecureString();
        /// <summary>
        /// Bound in code‐behind to PasswordBox.SecurePassword
        /// </summary>
        public SecureString Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        #endregion

        #region Commands

        private void NewProfile()
        {
            var np = new ConnectionProfile
            {
                Name = "New Profile",
                SourceVCenter = "",
                SourceUsername = ""
                // DestinationVCenter & DestinationUsername if you need them too
            };
            Profiles.Add(np);
            SelectedProfile = np;
        }

        private bool CanSaveProfile()
            => SelectedProfile != null
            && !string.IsNullOrWhiteSpace(SelectedProfile.Name)
            && !string.IsNullOrWhiteSpace(SelectedProfile.SourceVCenter)
            && !string.IsNullOrWhiteSpace(SelectedProfile.SourceUsername);

        private void SaveProfile()
        {
            // persist metadata
            _profileManager.SaveProfile(SelectedProfile);
            // persist secret
            _credentialManager.SavePassword(
                SelectedProfile.Name,
                SelectedProfile.SourceUsername,
                Password);
            LoadProfiles();
        }

        private bool CanDeleteProfile() => SelectedProfile != null;

        private void DeleteProfile()
        {
            _profileManager.DeleteProfile(SelectedProfile.Name);
            _credentialManager.DeletePassword(SelectedProfile.Name);
            LoadProfiles();
            SelectedProfile = null;
        }

        #endregion

        #region Helpers

        private void LoadProfiles()
        {
            Profiles.Clear();
            foreach (var p in _profileManager
                                .GetAllProfiles()
                                .OrderBy(x => x.Name))
            {
                Profiles.Add(p);
            }

            OnPropertyChanged(nameof(FilteredProfiles));
            SaveProfileCommand.RaiseCanExecuteChanged();
            DeleteProfileCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T field, T value,
            [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        #endregion
    }
}
