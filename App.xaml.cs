using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using UiDesktopApp2.ViewModels.Pages;
using UiDesktopApp2.ViewModels.Windows;
using UiDesktopApp2.Views.Pages;
using UiDesktopApp2.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;

namespace UiDesktopApp2
{
    public partial class App : Application
    {
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                var basePath = Path.GetDirectoryName(AppContext.BaseDirectory);
                if (basePath is null)
                {
                    throw new InvalidOperationException("Base directory path cannot be null.");
                }

                config.SetBasePath(basePath);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // App configuration
                var appConfig = context.Configuration.GetSection("AppConfig").Get<AppConfig>() ?? new AppConfig();
                services.AddSingleton(appConfig);

                // WPF UI services
                services.AddSingleton<INavigationViewPageProvider, NavigationViewPageProvider>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // Main window
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                // Pages and ViewModels
                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<DataPage>();
                services.AddSingleton<DataViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<LogsPage>();
                services.AddSingleton<LogsViewModel>();

                // Business services
                services.AddSingleton<PowerShellManager>();
                services.AddSingleton<ConnectionManager>();
                services.AddSingleton<IProfileManager, JsonProfileManager>();
            })
            .Build();

        public static IServiceProvider Services => _host.Services;

        private void OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                _host.StartAsync().Wait();

                // Get and show the main window
                var mainWindow = _host.Services.GetRequiredService<INavigationWindow>();
                mainWindow.ShowWindow();

                // Navigate to dashboard by default
                mainWindow.Navigate(typeof(DashboardPage));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error");
                Environment.Exit(1);
            }
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            _host.StopAsync().Wait();
            _host.Dispose();
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
