﻿namespace UiDesktopApp2.Models
{
    public class AppConfig
    {
        public string ConfigurationsFolder { get; set; }

        public string AppPropertiesFileName { get; set; }

        public string ScriptsFolder { get; set; }

        /// <summary>
        /// Where profiles.json lives if you want to override.
        /// </summary>
        public string ProfileStorePath { get; set; }
        // TODO: add any other settings you read from appsettings.json,
        // e.g. connection timeouts, log levels, etc.

        public string DefaultSourceVCenter { get; set; }
        public string DefaultDestinationVCenter { get; set; }
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public bool EnableDetailedLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "Logs";
    }
}
