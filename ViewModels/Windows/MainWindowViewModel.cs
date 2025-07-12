using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UiDesktopApp2.Models;
using UiDesktopApp2.Views.Pages;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;


namespace UiDesktopApp2.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle;

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new ObservableCollection<object>();

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new ObservableCollection<object>();

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new ObservableCollection<MenuItem>
            {
                new MenuItem { Header = "Home", Tag = "tray_home" }
            };

        public MainWindowViewModel(AppConfig appConfig)
        {
            _applicationTitle = $"{appConfig.ApplicationName} v{appConfig.Version}";

            // Build the left-pane menu
            MenuItems = new ObservableCollection<object>
                {
                    new NavigationViewItem
                    {
                        Content = "Dashboard",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                        TargetPageType = typeof(DashboardPage)
                    },
                    new NavigationViewItem
                    {
                        Content = "Connection",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.PlugConnected24 },
                        TargetPageType = typeof(ConnectionPage)  // You need to create this
                    },
                    new NavigationViewItem
                    {
                        Content = "Backup",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Save24 },
                        TargetPageType = typeof(BackupPage)  // You need to create this
                    },
                    new NavigationViewItem
                    {
                        Content = "Migration",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Rocket24 },
                        TargetPageType = typeof(MigrationPage)  // You need to create this
                    },
                    new NavigationViewItem
                    {
                        Content = "Validation",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Search24 },
                        TargetPageType = typeof(ValidationPage)  // You need to create this
                    },
                    new NavigationViewItem
                    {
                        Content = "Logs",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Document24 },
                        TargetPageType = typeof(LogsPage)  // You need to create this
                    },
                    new NavigationViewItem
                    {
                        Content = "Data",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                        TargetPageType = typeof(DataPage)
                    }
                };

            // Footer items
            FooterMenuItems = new ObservableCollection<object>
                {
                    new NavigationViewItem
                    {
                        Content = "Settings",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                        TargetPageType = typeof(SettingsPage)
                    },
                    new NavigationViewItem
                    {
                        Content = "About",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Info24 },
                        TargetPageType = typeof(AboutPage)  // You need to create this
                    }
                };
        }
    }
}
