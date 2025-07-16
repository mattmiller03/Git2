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

        protected override async void OnStartup(StartupEventArgs e)
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
                // Initialize ServiceLocator
                ServiceLocator.Initialize(_host.Services);
                await _host.StartAsync();

                // Register PowerShell scripts
                var scriptManager = _host.Services.GetRequiredService<IPowerShellScriptManager>();
                RegisterAllPowerShellScripts(scriptManager);

                // Get and show the main window
                var mainWindow = _host.Services.GetRequiredService<INavigationWindow>();
                mainWindow.ShowWindow();

                // Navigate to dashboard by default
                mainWindow.Navigate(typeof(DashboardPage));

                // Apply theme
                ApplyTheme(); // Apply theme based on appsettings.json
            }
            catch (Exception ex)
            {
                HandleCriticalStartupError(ex);
            }
        }

        public static void RegisterAllPowerShellScripts(IPowerShellScriptManager scriptManager)
        {
            try
            {
                string scriptsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
                if (!Directory.Exists(scriptsRoot))
                    throw new DirectoryNotFoundException($"Scripts folder not found: {scriptsRoot}");

                // Get all .ps1 files recursively
                var scriptFiles = Directory.GetFiles(scriptsRoot, "*.ps1", SearchOption.AllDirectories);

                foreach (var fullPath in scriptFiles)
                {
                    // Compute relative path from base directory
                    string relativePath = Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, fullPath);

                    // Use relative path with forward slashes for consistency (optional)
                    relativePath = relativePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    // Use filename without extension as script key
                    string scriptName = Path.GetFileNameWithoutExtension(fullPath);

                    scriptManager.RegisterScript(scriptName, relativePath);

                    // Optional: log registration
                    Console.WriteLine($"Registered PowerShell script: {scriptName} at {relativePath}");
                }
            }
            catch (Exception ex)
            {
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

        private static void ApplyTheme()
        {
            try
            {
                var appConfig = _host.Services.GetRequiredService<AppConfig>();
                var theme = appConfig.GetTheme();

                // Apply theme globally with Mica backdrop and update setting
                ApplicationThemeManager.Apply(
                    theme,
                    Wpf.Ui.Controls.WindowBackdropType.Mica,
                    true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme application error: {ex.Message}");
            }
        }


        private void HandleException(Exception ex, string source)
        {
            try
            {
                _logger?.LogError(ex, "[{Source}] Unhandled exception occurred", source);

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
            // Register MainWindow with factory to inject dependencies
            services.AddSingleton<INavigationWindow>(sp =>
            {
                var vm = sp.GetRequiredService<MainWindowViewModel>();
                var pageProvider = sp.GetRequiredService<INavigationViewPageProvider>();
                var navigationService = sp.GetRequiredService<INavigationService>();
                var themeService = sp.GetRequiredService<IThemeService>();

                return new MainWindow(vm, pageProvider, navigationService, themeService);
            });

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
    }
}
