using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
using Wpf.Ui.Converters;

namespace UiDesktopApp2.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "WPF UI - UiDesktopApp2";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
                    {
                        new NavigationViewItem()
                        {
                            Content = "Home",
                            Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                            TargetPageType = typeof(Views.Pages.DashboardPage)
                        },
                        new NavigationViewItem()
                        {
                            Content = "Data",
                            Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                            TargetPageType = typeof(Views.Pages.DataPage)
                        }
                    };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
                    {
                        new NavigationViewItem()
                        {
                            Content = "Settings",
                            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                            TargetPageType = typeof(Views.Pages.SettingsPage)
                        }
                    };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
                    {
                        new MenuItem { Header = "Home", Tag = "tray_home" }
                    };

        public MainWindowViewModel()
        {
            // build the left‐pane menu
            MenuItems.Add(new NavigationViewItem
            {
                Tag = "Dashboard",
                Content = "Dashboard",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 }
            });
            MenuItems.Add(new NavigationViewItem
            {
                Tag = "Connection",
                Content = "Connection",
                Icon = new SymbolIcon { Symbol = SymbolRegular.PlugConnected24 }
            });
            MenuItems.Add(new NavigationViewItem
            {
                Tag = "Backup",
                Content = "Backup",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Save24 }
            });
            MenuItems.Add(new NavigationViewItem
            {
                Tag = "Migration",
                Content = "Migration",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Rocket24 }
            });
            MenuItems.Add(new NavigationViewItem
            {
                Tag = "Validation",
                Content = "Validation",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Search24 }
            });
            MenuItems.Add(new NavigationViewItem
            {
                Tag = "Logs",
                Content = "Logs",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Document24 } // Fixed here
            });

            // example footer
            FooterMenuItems.Add(new NavigationViewItem
            {
                Tag = "About",
                Content = "About",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Info24 }
            });
        }
    }
}
