using System.Windows;
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
            Hide(ContentDialogResult.Primary);
        }

        protected override void OnPrimaryButtonClick(ContentDialogButtonClickEventArgs args)
        {
            if (ViewModel.SelectedProfile != null)
            {
                SelectedProfileResult = ViewModel.SelectedProfile;
                args.Result = ContentDialogResult.Primary;
            }
            else
            {
                args.Cancel = true;
                ViewModel.ShowStatusMessage("Selection Required", "Please select a profile to load", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            }
        }

        protected override void OnSecondaryButtonClick(ContentDialogButtonClickEventArgs args)
        {
            SelectedProfileResult = null;
            args.Result = ContentDialogResult.Secondary;
        }
    }
}
