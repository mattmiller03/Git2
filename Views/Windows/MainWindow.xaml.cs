using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using UiDesktopApp2.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using UiDesktopApp2.Models;

namespace UiDesktopApp2.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            IThemeService themeService)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            // Set navigation
            SetPageService(navigationViewPageProvider);
            navigationService.SetNavigationControl(RootNavigation);

            // Apply theme (using default theme)
            themeService.SetTheme(ApplicationTheme.Dark); // or ApplicationTheme.Light
        }

        #region INavigationWindow implementation
        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider)
            => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            // Implementation if needed
        }
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
    }
}
