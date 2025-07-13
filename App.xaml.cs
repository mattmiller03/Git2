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
using Wpf.Ui.Appearance;
using UiDesktopApp2.Views.Pages;
using UiDesktopApp2.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using SystemTheme = UiDesktopApp2.Helpers.SystemTheme;

namespace UiDesktopApp2
{
    public partial class App : Application
    {
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                var basePath = Path.GetDirectoryName(AppContext.BaseDirectory)
                    ?? throw new InvalidOperationException("Base directory path cannot be null.");

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

                // Pages and ViewModels - Core Application Pages
                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();

                services.AddSingleton<ConnectionPage>();
                services.AddSingleton<ConnectionViewModel>();

                services.AddSingleton<DataPage>();
                services.AddSingleton<DataViewModel>();

                services.AddSingleton<MigrationPage>();
                services.AddSingleton<MigrationViewModel>();

                services.AddSingleton<BackupPage>();
                services.AddSingleton<BackupViewModel>();

                services.AddSingleton<LogsPage>();
                services.AddSingleton<LogsViewModel>();

                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();

                services.AddSingleton<AboutPage>();
                services.AddSingleton<AboutViewModel>();

                // Business services
                services.AddSingleton<PowerShellManager>();
                services.AddSingleton<ConnectionManager>();
                services.AddSingleton<IProfileManager, JsonProfileManager>();


                // Additional services for enhanced functionality
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();
            })
            .Build();

        public static IServiceProvider Services => _host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await _host.StartAsync();

                // Initialize logging
                InitializeLogging();

                // Get and show the main window
                var mainWindow = _host.Services.GetRequiredService<INavigationWindow>();
                mainWindow.ShowWindow();

                // Navigate to dashboard by default
                mainWindow.Navigate(typeof(DashboardPage));

                // Apply theme based on system preference
                ApplyTheme();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start application: {ex.Message}\n\nDetails: {ex}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Environment.Exit(1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                // Save any pending changes
                await SaveApplicationStateAsync();

                await _host.StopAsync();
                _host.Dispose();
            }
            catch (Exception ex)
            {
                // Log error but don't prevent shutdown
                System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                // Log the exception
                LogException(e.Exception);

                var message = $"An unhandled exception occurred:\n\n{e.Exception.Message}";

                if (e.Exception.InnerException != null)
                {
                    message += $"\n\nInner Exception: {e.Exception.InnerException.Message}";
                }

                var result = MessageBox.Show(
                    $"{message}\n\nWould you like to continue running the application?",
                    "Unhandled Exception",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                e.Handled = result == MessageBoxResult.Yes;
            }
            catch
            {
                // If we can't even show the error dialog, just mark as handled
                e.Handled = true;
            }
        }

        private void InitializeLogging()
        {
            try
            {
                var appConfig = _host.Services.GetRequiredService<AppConfig>();
                var logDirectory = Path.GetFullPath(appConfig.LogPath);

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Set up global exception handling
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                DispatcherUnhandledException += OnDispatcherUnhandledException;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize logging: {ex.Message}");
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                LogException(exception);

                MessageBox.Show(
                    $"A fatal error occurred: {exception.Message}\n\nThe application will now exit.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
            }
        }

        private void ApplyTheme()
        {
            try
            {
                // Get the main window
                var mainWindow = _host.Services.GetRequiredService<INavigationWindow>();

                // Apply dark theme dynamically
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                    Wpf.Ui.Appearance.ApplicationTheme.Dark,
                    Wpf.Ui.Controls.WindowBackdropType.Mica,
                    true
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to apply theme: {ex.Message}");
            }
        }

        private static async Task SaveApplicationStateAsync()
        {
            try
            {
                // Save any pending configuration changes
                var appConfig = _host.Services.GetRequiredService<AppConfig>();

                // In a real implementation, you might save user preferences, window positions, etc.
                await Task.Delay(100); // Placeholder for actual save operations
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save application state: {ex.Message}");
            }
        }

        private static void LogException(Exception exception)
        {
            try
            {
                var appConfig = _host.Services.GetRequiredService<AppConfig>();
                var logFile = Path.Combine(appConfig.LogPath, $"error_{DateTime.Now:yyyy-MM-dd}.log");

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {exception}\n\n";
                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // If we can't log to file, at least output to debug
                System.Diagnostics.Debug.WriteLine($"Exception: {exception}");
            }
        }

        public static T GetService<T>() where T : class
        {
            return _host.Services.GetService<T>() ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} not found.");
        }

        public static object GetService(Type serviceType)
        {
            return _host.Services.GetService(serviceType) ?? throw new InvalidOperationException($"Service of type {serviceType.Name} not found.");
        }
    }
}
