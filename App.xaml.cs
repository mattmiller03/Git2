using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UiDesktopApp2.Services;
using UiDesktopApp2.ViewModels.Pages;
using UiDesktopApp2.ViewModels.Windows;
using UiDesktopApp2.Views.Pages;
using UiDesktopApp2.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using Wpf.Ui.Abstractions;
using System.Windows;

namespace UiDesktopApp2
{
    /// <summary>  
    /// Interaction logic for App.xaml  
    /// </summary>  
    public partial class App : Application
    {
        /// <summary>  
        /// Occurs when the application is loading.  
        /// </summary>  
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Resolve dependencies for MainWindow constructor  
            var serviceProvider = CreateServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
            var pageProvider = serviceProvider.GetRequiredService<INavigationViewPageProvider>();
            var navigationService = serviceProvider.GetRequiredService<INavigationService>();

            // Create and show MainWindow with required parameters  
            var win = new Views.Windows.MainWindow(viewModel, pageProvider, navigationService);
            win.Show();
        }

        private IServiceProvider CreateServiceProvider()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register required services  
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<INavigationViewPageProvider, NavigationViewPageProvider>();
                    services.AddSingleton<INavigationService, NavigationService>();
                })
                .Build();

            return host.Services;
        }
    }
}
