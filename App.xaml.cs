using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace UiDesktopApp2
{
    public partial class App : Application
    {
        private static readonly IHost _host;

        static App()
        {
            try
            {
                _host = Host
                    .CreateDefaultBuilder()
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddDebug();
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Trace);
                    })
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        var basePath = Path.GetDirectoryName(AppContext.BaseDirectory)
                            ?? throw new InvalidOperationException("Base directory path cannot be null.");

                        config.SetBasePath(basePath);
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(context, services);
                    })
                    .Build();
            }
            catch (Exception ex)
            {
                LogStartupError(ex);
                throw;
            }
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            try
            {
                // App configuration
                var appConfig = context.Configuration.GetSection("AppConfig").Get<AppConfig>() ?? new AppConfig();
                services.AddSingleton(appConfig);

                // Logging
                services.AddLogging();

                // WPF UI services
                services.AddSingleton<INavigationViewPageProvider, NavigationViewPageProvider>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // Main window and navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                // Pages and ViewModels
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

                // Services
                services.AddSingleton<ILogManager, LogManager>();
                services.AddSingleton<IPowerShellScriptManager, PowerShellScriptManager>();
                services.AddSingleton<ConnectionManager>();
                services.AddSingleton<IProfileManager, JsonProfileManager>();

                // Hosted services
                services.AddHostedService<ApplicationHostService>();
            }
            catch (Exception ex)
            {
                LogStartupError(ex);
                throw;
            }
        }

        private static void LogStartupError(Exception ex)
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VCenterMigrationTool",
                "startup_error.log"
            );

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

                File.WriteAllText(logPath, $@"Startup Error at {DateTime.Now}:
Exception Type: {ex.GetType().FullName}
Message: {ex.Message}
Stack Trace: {ex.StackTrace}

Inner Exception:
{ex.InnerException?.Message ?? "No inner exception"}");

                // Also output to debug console
                System.Diagnostics.Debug.WriteLine($"Startup Error: {ex}");
            }
            catch
            {
                // Fallback error logging
                System.Diagnostics.Debug.WriteLine($"Critical Startup Error: {ex}");
            }
        }

        public static IServiceProvider Services => _host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                await _host.StartAsync();

                // Register PowerShell scripts
                var scriptManager = _host.Services.GetRequiredService<IPowerShellScriptManager>();
                RegisterPowerShellScripts(scriptManager);

                // Get and show the main window
                var mainWindow = _host.Services.GetRequiredService<INavigationWindow>();
                mainWindow.ShowWindow();

                // Navigate to dashboard by default
                mainWindow.Navigate(typeof(DashboardPage));

                // Apply theme
                ApplyTheme();
            }
            catch (Exception ex)
            {
                LogStartupError(ex);

                MessageBox.Show(
                    $"Failed to start application: {ex.Message}\n\nDetails: {ex}",
                    "Startup Error",
                    System.Windows.MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown(1);
            }
        }

        private void RegisterPowerShellScripts(IPowerShellScriptManager scriptManager)
        {
            try
            {
                scriptManager.RegisterScript(
                    "Set-Dyn_Env_TagPermissions",
                    @"Scripts\Set-Dyn_Env_TagPermissions.ps1"
                );

                // Add other script registrations as needed
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Script registration error: {ex.Message}");
            }
        }

        private void ApplyTheme()
        {
            try
            {
                var themeService = _host.Services.GetRequiredService<IThemeService>();

                // Alternative ways to set theme
                themeService.SetTheme(Wpf.Ui.Appearance.ApplicationTheme.Dark);
                // OR
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                    Wpf.Ui.Appearance.ApplicationTheme.Dark,
                    Wpf.Ui.Controls.WindowBackdropType.Mica,
                    true
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme application error: {ex.Message}");
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}
