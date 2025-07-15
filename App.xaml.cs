using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
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
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace UiDesktopApp2
{
    public partial class App : Application
    {
        private static readonly IHost _host;
        private ILogger<App>? _logger;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize logger
            _logger = _host.Services.GetService<ILogger<App>>();

            // Setup global exception handling
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
                #pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                #pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

            try
            {
                _host.Start();

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
                HandleCriticalStartupError(ex);
            }
        }

        public void RegisterPowerShellScripts(IPowerShellScriptManager scriptManager)
        {
            try
            {
                // Verify script exists before registering
                string scriptPath = Path.Combine("Scripts", "Set-Dyn_Env_TagPermissions.ps1");
                string fullPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath));

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Critical error: PowerShell script not found at {fullPath}");
                }

                scriptManager.RegisterScript("Set-Dyn_Env_TagPermissions", scriptPath);
            }
            catch (Exception ex)
            {
                // This will fail fast if scripts are missing
                MessageBox.Show($"FATAL ERROR: Could not register PowerShell scripts.\n{ex.Message}",
                              "Startup Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            HandleException(e.Exception, "UI Thread");
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception)e.ExceptionObject, "AppDomain");
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            HandleException(e.Exception, "Background Task");
        }

        private void HandleException(Exception ex, string source)
        {
            try
            {
                _logger?.LogError(ex, $"[{source}] Unhandled exception occurred");

                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{ex.Message}\n\nCheck logs for details.",
                    "Application Error",
                    System.Windows.MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to handle exception: {logEx}");
                System.Diagnostics.Debug.WriteLine($"Original exception: {ex}");
            }
        }

        private void HandleCriticalStartupError(Exception ex)
        {
            LogStartupError(ex);
            MessageBox.Show(
                $"Failed to start application: {ex.Message}\n\nDetails: {ex}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // App config
            var appConfig = context.Configuration.GetSection("AppConfig").Get<AppConfig>() ?? new AppConfig();
            services.AddSingleton(appConfig);

            // Logging
            services.AddLogging();

            // WPF UI services
            services.AddSingleton<INavigationViewPageProvider, NavigationViewPageProvider>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ITaskBarService, TaskBarService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Windows and viewmodels
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<INavigationWindow, MainWindow>();

            // Pages and viewmodels
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
            // Profile Management
            services.AddSingleton<IProfileManager, JsonProfileManager>();


            // Application services
            services.AddSingleton<ILogManager, LogManager>();
            services.AddSingleton<IPowerShellScriptManager, PowerShellScriptManager>();
            services.AddSingleton<PowerShellManager>();
            services.AddSingleton<ICredentialManager, WindowsCredentialManager>();
            services.AddSingleton<ConnectionManager>();

            // Hosted services
            services.AddHostedService<ApplicationHostService>();
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

        private void ApplyTheme()
        {
            try
            {
                var themeService = _host.Services.GetRequiredService<IThemeService>();

                themeService.SetTheme(Wpf.Ui.Appearance.ApplicationTheme.Dark);
                // Or use ApplicationThemeManager.Apply if preferred
                // Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                //     Wpf.Ui.Appearance.ApplicationTheme.Dark,
                //     Wpf.Ui.Controls.WindowBackdropType.Mica,
                //     true
                // );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme application error: {ex.Message}");
            }
        }
    }
}
