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

            // Handle the dialog result manually
            Loaded += OnDialogLoaded;
        }

        private void OnDialogLoaded(object sender, RoutedEventArgs e)
        {
            // Find and handle the primary button click
            if (FindName("PrimaryButton") is System.Windows.Controls.Button primaryButton)
            {
                primaryButton.Click += OnPrimaryButtonClick;
            }

            if (FindName("SecondaryButton") is System.Windows.Controls.Button secondaryButton)
            {
                secondaryButton.Click += OnSecondaryButtonClick;
            }
        }

        private void OnProfileSelected(ConnectionProfile profile)
        {
            SelectedProfileResult = profile;
            Hide();
        }

        private void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedProfile != null)
            {
                SelectedProfileResult = ViewModel.SelectedProfile;
                Hide();
            }
            else
            {
                ViewModel.ShowStatusMessage("Selection Required", "Please select a profile to load", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            }
        }

        private void OnSecondaryButtonClick(object sender, RoutedEventArgs e)
        {
            SelectedProfileResult = null;
            Hide();
        }
    }
}
