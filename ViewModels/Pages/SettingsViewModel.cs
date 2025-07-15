using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly ILogger<SettingsViewModel> _logger;
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        public SettingsViewModel(ILogger<SettingsViewModel> logger)
        {
            _logger = logger;
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                InitializeViewModel();
            }

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            try
            {
                CurrentTheme = ApplicationThemeManager.GetAppTheme();
                AppVersion = $"UiDesktopApp2 - {GetAssemblyVersion()}";
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing SettingsViewModel");
            }
        }

        private static string GetAssemblyVersion()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting assembly version: {ex.Message}");
                return string.Empty;
            }
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            try
            {
                switch (parameter)
                {
                    case "theme_light":
                        if (CurrentTheme == ApplicationTheme.Light)
                            break;

                        ApplicationThemeManager.Apply(ApplicationTheme.Light, Wpf.Ui.Controls.WindowBackdropType.Mica, true);
                        CurrentTheme = ApplicationTheme.Light;
                        break;

                    default:
                        if (CurrentTheme == ApplicationTheme.Dark)
                            break;

                        ApplicationThemeManager.Apply(ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.Mica, true);
                        CurrentTheme = ApplicationTheme.Dark;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing theme to {parameter}");
            }
        }
    }
}
