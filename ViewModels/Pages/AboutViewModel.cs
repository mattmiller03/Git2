using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class AboutViewModel : ObservableObject
    {
        private readonly AppConfig _appConfig;

        [ObservableProperty]
        private string _applicationName = "vCenter Migration Tool";

        [ObservableProperty]
        private string _applicationVersion = "1.0.0";

        [ObservableProperty]
        private string _buildVersion = "1.0.0.0";

        [ObservableProperty]
        private string _buildDate = string.Empty;

        [ObservableProperty]
        private string _copyright = "© 2024 Your Company Name. All rights reserved.";

        [ObservableProperty]
        private string _description = "A comprehensive tool for migrating virtual machines between vCenter environments with advanced features and monitoring capabilities.";

        [ObservableProperty]
        private string _companyName = "Your Company Name";

        [ObservableProperty]
        private string _companyWebsite = "https://www.yourcompany.com";

        [ObservableProperty]
        private string _supportEmail = "support@yourcompany.com";

        [ObservableProperty]
        private string _documentationUrl = "https://docs.yourcompany.com/vcenter-migration-tool";

        // System Information
        [ObservableProperty]
        private string _operatingSystem = string.Empty;

        [ObservableProperty]
        private string _dotNetVersion = string.Empty;

        [ObservableProperty]
        private string _processorArchitecture = string.Empty;

        [ObservableProperty]
        private string _installedMemory = string.Empty;

        [ObservableProperty]
        private string _applicationPath = string.Empty;

        [ObservableProperty]
        private string _configurationPath = string.Empty;

        [ObservableProperty]
        private string _logPath = string.Empty;

        // Component Versions
        [ObservableProperty]
        private ObservableCollection<ComponentInfo> _componentVersions = new();

        // License Information
        [ObservableProperty]
        private string _licenseType = "Commercial License";

        [ObservableProperty]
        private string _licenseExpiry = string.Empty;

        [ObservableProperty]
        private string _licensedTo = string.Empty;

        [ObservableProperty]
        private bool _isLicenseValid = true;

        // Features
        [ObservableProperty]
        private ObservableCollection<FeatureInfo> _availableFeatures = new();

        // Third-party Components
        [ObservableProperty]
        private ObservableCollection<ThirdPartyComponent> _thirdPartyComponents = new();

        // Change Log
        [ObservableProperty]
        private ObservableCollection<ChangeLogEntry> _recentChanges = new();

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public AboutViewModel(AppConfig appConfig)
        {
            _appConfig = appConfig;

            LoadApplicationInfo();
            LoadSystemInfo();
            LoadComponentVersions();
            LoadFeatureInfo();
            LoadThirdPartyComponents();
            LoadChangeLog();
        }

        [RelayCommand]
        private void OpenWebsite()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = CompanyWebsite,
                    UseShellExecute = true
                });
                StatusMessage = "Opened company website";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open website: {ex.Message}";
            }
        }

        [RelayCommand]
        private void OpenDocumentation()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DocumentationUrl,
                    UseShellExecute = true
                });
                StatusMessage = "Opened documentation";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open documentation: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SendSupportEmail()
        {
            try
            {
                var subject = Uri.EscapeDataString($"Support Request - {ApplicationName} v{ApplicationVersion}");
                var body = Uri.EscapeDataString($@"
Application: {ApplicationName}
Version: {ApplicationVersion}
Build: {BuildVersion}
OS: {OperatingSystem}
.NET: {DotNetVersion}

Please describe your issue:


");

                var mailto = $"mailto:{SupportEmail}?subject={subject}&body={body}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = mailto,
                    UseShellExecute = true
                });
                StatusMessage = "Opened email client for support request";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open email client: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CopySystemInfo()
        {
            try
            {
                var systemInfo = $@"
{ApplicationName} System Information
=====================================

Application Information:
- Name: {ApplicationName}
- Version: {ApplicationVersion}
- Build: {BuildVersion}
- Build Date: {BuildDate}
- Path: {ApplicationPath}

System Information:
- OS: {OperatingSystem}
- .NET Version: {DotNetVersion}
- Architecture: {ProcessorArchitecture}
- Memory: {InstalledMemory}

Configuration:
- Config Path: {ConfigurationPath}
- Log Path: {LogPath}

Component Versions:
{string.Join("\n", ComponentVersions.Select(c => $"- {c.Name}: {c.Version}"))}

License Information:
- Type: {LicenseType}
- Licensed To: {LicensedTo}
- Valid: {(IsLicenseValid ? "Yes" : "No")}
{(string.IsNullOrEmpty(LicenseExpiry) ? "" : $"- Expires: {LicenseExpiry}")}
";

                System.Windows.Clipboard.SetText(systemInfo);
                StatusMessage = "System information copied to clipboard";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to copy system info: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CheckForUpdates()
        {
            try
            {
                StatusMessage = "Checking for updates...";

                // In a real implementation, this would check for updates
                Task.Run(async () =>
                {
                    await Task.Delay(2000); // Simulate check

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "You are running the latest version";
                    });
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update check failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ViewLicense()
        {
            try
            {
                var licenseText = GetLicenseText();

                // In a real implementation, this would show a license dialog
                var tempFile = Path.Combine(Path.GetTempPath(), "license.txt");
                File.WriteAllText(tempFile, licenseText);

                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                });

                StatusMessage = "License agreement opened";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to view license: {ex.Message}";
            }
        }

        [RelayCommand]
        private void OpenApplicationFolder()
        {
            try
            {
                var folder = Path.GetDirectoryName(ApplicationPath) ?? Environment.CurrentDirectory;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = folder,
                    UseShellExecute = true
                });
                StatusMessage = "Opened application folder";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open folder: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ExportSystemReport()
        {
            try
            {
                var fileName = $"System_Report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.html";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                GenerateSystemReport(filePath);
                StatusMessage = $"System report exported to {filePath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        private void LoadApplicationInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();

            ApplicationName = _appConfig.ApplicationName;
            ApplicationVersion = _appConfig.Version;
            BuildVersion = assemblyName.Version?.ToString() ?? "Unknown";

            // Get build date from assembly
            var buildDate = File.GetLastWriteTime(assembly.Location);
            BuildDate = buildDate.ToString("yyyy-MM-dd HH:mm:ss");

            ApplicationPath = assembly.Location;

            // Use the actual paths from AppConfig
            ConfigurationPath = Path.Combine(Environment.CurrentDirectory, "appsettings.json");
            LogPath = _appConfig.LogPath;

            // License info (would typically come from a license file or registry)
            LicensedTo = Environment.UserName;
            LicenseExpiry = "Perpetual";
        }

        private void LoadSystemInfo()
        {
            OperatingSystem = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
            DotNetVersion = Environment.Version.ToString();
            ProcessorArchitecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

            // Get memory info
            var totalMemory = GC.GetTotalMemory(false);
            InstalledMemory = $"{totalMemory / (1024 * 1024)} MB allocated";
        }

        private void LoadComponentVersions()
        {
            ComponentVersions.Clear();

            try
            {
                // WPF UI Framework
                var wpfUiAssembly = Assembly.LoadFrom("Wpf.Ui.dll");
                ComponentVersions.Add(new ComponentInfo
                {
                    Name = "WPF UI Framework",
                    Version = wpfUiAssembly.GetName().Version?.ToString() ?? "Unknown",
                    Description = "Modern WPF UI framework"
                });
            }
            catch
            {
                ComponentVersions.Add(new ComponentInfo
                {
                    Name = "WPF UI Framework",
                    Version = "Unknown",
                    Description = "Modern WPF UI framework"
                });
            }

            // Add other component versions
            ComponentVersions.Add(new ComponentInfo
            {
                Name = "Community Toolkit MVVM",
                Version = "8.2.0",
                Description = "MVVM toolkit for .NET"
            });

            ComponentVersions.Add(new ComponentInfo
            {
                Name = "PowerShell Core",
                Version = "7.4.0",
                Description = "PowerShell runtime for vCenter operations"
            });

            ComponentVersions.Add(new ComponentInfo
            {
                Name = "VMware PowerCLI",
                Version = "13.2.1",
                Description = "VMware vSphere PowerShell module"
            });
        }

        private void LoadFeatureInfo()
        {
            AvailableFeatures.Clear();

            AvailableFeatures.Add(new FeatureInfo { Name = "Cross-vCenter Migration", IsEnabled = true, Description = "Migrate VMs between different vCenter servers" });
            AvailableFeatures.Add(new FeatureInfo { Name = "Bulk Migration", IsEnabled = true, Description = "Migrate multiple VMs simultaneously" });
            AvailableFeatures.Add(new FeatureInfo { Name = "Progress Monitoring", IsEnabled = true, Description = "Real-time migration progress tracking" });
            AvailableFeatures.Add(new FeatureInfo { Name = "Pre-migration Validation", IsEnabled = true, Description = "Validate VMs before migration" });
            AvailableFeatures.Add(new FeatureInfo { Name = "Network Mapping", IsEnabled = true, Description = "Automatic network remapping" });
            AvailableFeatures.Add(new FeatureInfo { Name = "Scheduled Migrations", IsEnabled = true, Description = "Schedule migrations for later execution" });
            AvailableFeatures.Add(new FeatureInfo { Name = "Migration Rollback", IsEnabled = false, Description = "Rollback failed migrations" });
            AvailableFeatures.Add(new FeatureInfo { Name = "Advanced Reporting", IsEnabled = true, Description = "Detailed migration reports and analytics" });
        }

        private void LoadThirdPartyComponents()
        {
            ThirdPartyComponents.Clear();

            ThirdPartyComponents.Add(new ThirdPartyComponent
            {
                Name = "VMware vSphere PowerCLI",
                Version = "13.2.1",
                License = "VMware EULA",
                Website = "https://developer.vmware.com/powercli"
            });

            ThirdPartyComponents.Add(new ThirdPartyComponent
            {
                Name = "WPF UI",
                Version = "3.0.0",
                License = "MIT License",
                Website = "https://github.com/lepoco/wpfui"
            });

            ThirdPartyComponents.Add(new ThirdPartyComponent
            {
                Name = "CommunityToolkit.Mvvm",
                Version = "8.2.0",
                License = "MIT License",
                Website = "https://github.com/CommunityToolkit/dotnet"
            });

            ThirdPartyComponents.Add(new ThirdPartyComponent
            {
                Name = "Microsoft.Extensions.Hosting",
                Version = "8.0.0",
                License = "MIT License",
                Website = "https://github.com/dotnet/runtime"
            });
        }

        private void LoadChangeLog()
        {
            RecentChanges.Clear();

            RecentChanges.Add(new ChangeLogEntry
            {
                Version = "1.0.0",
                Date = DateTime.Now.AddDays(-1),
                Type = "Release",
                Description = "Initial release with core migration functionality"
            });

            RecentChanges.Add(new ChangeLogEntry
            {
                Version = "0.9.5",
                Date = DateTime.Now.AddDays(-7),
                Type = "Feature",
                Description = "Added bulk migration support and progress monitoring"
            });

            RecentChanges.Add(new ChangeLogEntry
            {
                Version = "0.9.0",
                Date = DateTime.Now.AddDays(-14),
                Type = "Feature",
                Description = "Implemented network mapping and validation features"
            });

            RecentChanges.Add(new ChangeLogEntry
            {
                Version = "0.8.5",
                Date = DateTime.Now.AddDays(-21),
                Type = "Bug Fix",
                Description = "Fixed issues with PowerCLI connection management"
            });
        }

        private string GetLicenseText()
        {
            return @"
SOFTWARE LICENSE AGREEMENT

This software is licensed under commercial terms.

1. GRANT OF LICENSE
Subject to the terms of this Agreement, we grant you a non-exclusive, 
non-transferable license to use this software.

2. RESTRICTIONS
You may not reverse engineer, decompile, or disassemble the software.

3. SUPPORT
Support is provided via email at support@yourcompany.com

4. WARRANTY DISCLAIMER
THIS SOFTWARE IS PROVIDED 'AS IS' WITHOUT ANY WARRANTY.

For complete license terms, visit: https://www.yourcompany.com/license
";
        }

        private void GenerateSystemReport(string filePath)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{ApplicationName} - System Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
        .section {{ margin: 20px 0; }}
        .component {{ background: #f8f9fa; padding: 10px; margin: 5px 0; border-radius: 5px; }}
        table {{ width: 100%; border-collapse: collapse; }}
        th, td {{ padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{ApplicationName} - System Report</h1>
        <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    
    <div class='section'>
        <h2>Application Information</h2>
        <table>
            <tr><th>Property</th><th>Value</th></tr>
            <tr><td>Name</td><td>{ApplicationName}</td></tr>
            <tr><td>Version</td><td>{ApplicationVersion}</td></tr>
            <tr><td>Build</td><td>{BuildVersion}</td></tr>
            <tr><td>Build Date</td><td>{BuildDate}</td></tr>
            <tr><td>Path</td><td>{ApplicationPath}</td></tr>
        </table>
    </div>
    
    <div class='section'>
        <h2>System Information</h2>
        <table>
            <tr><th>Property</th><th>Value</th></tr>
            <tr><td>Operating System</td><td>{OperatingSystem}</td></tr>
            <tr><td>.NET Version</td><td>{DotNetVersion}</td></tr>
            <tr><td>Architecture</td><td>{ProcessorArchitecture}</td></tr>
            <tr><td>Memory</td><td>{InstalledMemory}</td></tr>
        </table>
    </div>
    
    <div class='section'>
        <h2>Component Versions</h2>
        {string.Join("", ComponentVersions.Select(c => $"<div class='component'><strong>{c.Name}</strong> v{c.Version}<br/>{c.Description}</div>"))}
    </div>
    
    <div class='section'>
        <h2>Available Features</h2>
        {string.Join("", AvailableFeatures.Select(f => $"<div class='component'><strong>{f.Name}</strong> {(f.IsEnabled ? "✓" : "✗")}<br/>{f.Description}</div>"))}
    </div>
</body>
</html>";

            File.WriteAllText(filePath, html);
        }
    }
}
