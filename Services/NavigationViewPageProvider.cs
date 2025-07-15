using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Services;
using UiDesktopApp2.Views.Pages;
using Wpf.Ui.Abstractions;

public class NavigationViewPageProvider : INavigationViewPageProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _pageMap = new();

    public NavigationViewPageProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Register page types here
        _pageMap.Add(typeof(DashboardPage), typeof(DashboardPage));
        _pageMap.Add(typeof(ConnectionPage), typeof(ConnectionPage));
        _pageMap.Add(typeof(MigrationPage), typeof(MigrationPage));
        _pageMap.Add(typeof(SettingsPage), typeof(SettingsPage));
        _pageMap.Add(typeof(DataPage), typeof(DataPage));
        _pageMap.Add(typeof(BackupPage), typeof(BackupPage));
        _pageMap.Add(typeof(LogsPage), typeof(LogsPage));
        _pageMap.Add(typeof(AboutPage), typeof(AboutPage));
        _pageMap.Add(typeof(ValidationPage), typeof(ValidationPage));
        // ... other pages
    }

    public object? GetPage(Type pageType)
    {
        if (_pageMap.TryGetValue(pageType, out var implementationType))
        {
            return _serviceProvider.GetService(implementationType);
        }
        return null;
    }
}

