using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using UiDesktopApp2.ViewModels.Pages;
using UiDesktopApp2.ViewModels.Windows;
using UiDesktopApp2.Views.Pages;
using UiDesktopApp2.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.DependencyInjection;
using Wpf.Ui.Extensions;
using System;
using System.Windows;

namespace UiDesktopApp2
{
    /// <summary>  
    /// Interaction logic for App.xaml  
    /// </summary>  
    public partial class App : Application
    {
        private IHost? _host;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Build and start the generic Host
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Ensure the correct namespace is imported for AddWpfUi

                    // View / view-model registrations
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainWindowViewModel>();

                    // Navigation services
                    services.AddSingleton<INavigationViewPageProvider, INavigationViewPageProvider>();
                    services.AddSingleton<INavigationService, NavigationService>();

                    // Other project services
                    services.AddSingleton<PowerShellManager>();
                    services.AddSingleton<ConnectionManager>();
                    services.AddSingleton<JsonProfileManager>();
                })
                .Build();

            _host.Start();

            // Resolve and show the main window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            base.OnExit(e);
        }
    }
}
