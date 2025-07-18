using System;
using System.Windows;
using System.Windows.Controls;
using UiDesktopApp2.Models;
using UiDesktopApp2.ViewModels.Dialogs;
using Wpf.Ui.Controls;


namespace UiDesktopApp2.Views.Dialogs
{
    public partial class ProfileManagementDialog : ContentDialog
    {
        public ProfileManagementViewModel ViewModel { get; }
        public ConnectionProfile? SelectedProfileResult { get; private set; }

        public ProfileManagementDialog(ProfileManagementViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();

            // Subscribe to profile selection
            ViewModel.ProfileSelected += OnProfileSelected;
        }

        private void OnProfileSelected(ConnectionProfile profile)
        {
            SelectedProfileResult = profile;
            Hide();
        }

        // Add this missing method
        public ConnectionProfile? GetSelectedProfile()
        {
            return ViewModel.SelectedProfile;
        }
    }
}
