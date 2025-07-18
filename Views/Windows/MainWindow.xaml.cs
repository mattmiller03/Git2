using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using UiDesktopApp2.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace UiDesktopApp2.Views.Windows
{
    public partial class MainWindow : FluentWindow, INavigationWindow
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISnackbarService _snackbarService;

        public MainWindow(
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            ISnackbarService snackbarService)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _snackbarService = snackbarService;

            InitializeComponent();

            // Set up services
            SetupServices();

            // Navigate to dashboard on startup
            NavigateToPage(typeof(DashboardPage));
        }

        private void SetupServices()
        {
            // Set up navigation service
            _navigationService.SetNavigationControl(NavigationView);

            // Set up snackbar service
            _snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        }

        #region INavigationWindow Implementation

        public INavigationView GetNavigation() => NavigationView;

        public bool Navigate(Type pageType) => NavigateToPage(pageType);

        public void SetPageService(INavigationViewPageProvider pageService)
        {
            // Implementation for page service
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            // Implementation for service provider
        }

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion

        private bool NavigateToPage(Type pageType)
        {
            try
            {
                var page = _serviceProvider.GetService(pageType);
                if (page != null)
                {
                    NavigationFrame.Navigate(page);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                return false;
            }
        }
    }
}
