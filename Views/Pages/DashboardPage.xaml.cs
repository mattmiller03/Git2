using System;
using System.Windows.Controls;
using UiDesktopApp2.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp2.Views.Pages
{
    public partial class DashboardPage : Page, INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();

            // Load data when page loads
            Loaded += async (s, e) => await ViewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
