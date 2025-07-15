using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Services;
using Wpf.Ui.Appearance;



namespace UiDesktopApp2.Models
{
    public class AppConfig
    {
        public string ApplicationName { get; set; } = "vCenter Migration Tool";
        public string Version { get; set; } = "1.0.0";
        public int DefaultTimeout { get; set; } = 300;
        public int MaxConcurrentMigrations { get; set; } = 5;
        public string LogPath { get; set; } = "logs";
        public string ProfilesPath { get; set; } = "profiles";
        public bool EnableDetailedLogging { get; set; } = true;
        public string DefaultSourceVCenter { get; set; } = string.Empty;
        public string DefaultDestinationVCenter { get; set; } = string.Empty;

        public string ThemeName { get; set; } = "Unknown";
        public ApplicationTheme GetTheme()
        {
            return ThemeName?.ToLower() switch
            {
                "dark" => ApplicationTheme.Dark,
                "light" => ApplicationTheme.Light,
                _ => ApplicationTheme.Unknown
            };
        }
    }
}
