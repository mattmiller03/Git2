using System;
using Microsoft.Win32;

namespace UiDesktopApp2.Helpers
{
    public enum SystemTheme
    {
        Light,
        Dark
    }

    public static class SystemThemeDetector
    {
        public static SystemTheme DetectTheme()
        {
            try
            {
                // Windows Registry method for theme detection
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var registryValue = key?.GetValue("AppsUseLightTheme");
                    if (registryValue != null)
                    {
                        return (int)registryValue == 0 ? SystemTheme.Dark : SystemTheme.Light;
                    }
                }

                // Fallback to light theme
                return SystemTheme.Light;
            }
            catch
            {
                return SystemTheme.Light;
            }
        }

        public static bool IsSystemInDarkMode() => DetectTheme() == SystemTheme.Dark;
    }
}