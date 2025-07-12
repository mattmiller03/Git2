using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ConnectionManager _connectionManager;
        private readonly PowerShellManager _powerShellManager;
        private readonly AppConfig _appConfig;

        [ObservableProperty]
        private string _welcomeMessage = "Welcome back! Here's your migration overview.";

        // Connection Status
        [ObservableProperty]
        private string _sourceVcenterName = "vcenter01.domain.com";

        [ObservableProperty]
        private string _sourceConnectionStatus = "Connected";

        [ObservableProperty]
        private bool _sourceIsConnected = true;

        [ObservableProperty]
        private int _sourceVmCount = 247;

        [ObservableProperty]
        private int _sourceHostCount = 12;

        [ObservableProperty]
        private int _sourceClusterCount = 3;

        [ObservableProperty]
        private string _destinationVcenterName = "vcenter02.domain.com";

        [ObservableProperty]
        private string _destinationConnectionStatus = "Connected";

        [ObservableProperty]
        private bool _destinationIsConnected = true;

        [ObservableProperty]
        private int _destinationVmCount = 89;

        [ObservableProperty]
        private int _destinationHostCount = 8;

        [ObservableProperty]
        private int _destinationClusterCount = 2;

        // Migration Statistics
        [ObservableProperty]
        private int _totalMigrations = 24;

        [ObservableProperty]
        private int _successfulMigrations = 21;

        [ObservableProperty]
        private int _failedMigrations = 2;

        [ObservableProperty]
        private int _inProgressMigrations = 1;

        // Current Activity
        [ObservableProperty]
        private ObservableCollection<ActiveMigration> _activeMigrations = new();

        [ObservableProperty]
        private int _queuedMigrations = 3;

        // Recent Migrations
        [ObservableProperty]
        private ObservableCollection<RecentMigration> _recentMigrations = new();

        // System Health
        [ObservableProperty]
        private string _powerCliStatus = "Connected";

        [ObservableProperty]
        private bool _powerCliIsHealthy = true;

        [ObservableProperty]
        private string _powerCliVersion = "Version 13.2.1";

        [ObservableProperty]
        private double _memoryUsagePercentage = 34.0;

        [ObservableProperty]
        private string _memoryUsageText = "346 MB / 1024 MB";

        [ObservableProperty]
        private string _logFileSize = "15.7 MB";

        [ObservableProperty]
        private string _logStatus = "Archive recommended";

        [ObservableProperty]
        private bool _logNeedsAttention = true;

        // Status
        [ObservableProperty]
        private string _systemStatusMessage = "All systems operational. Last updated: 2 minutes ago";

        [ObservableProperty]
        private string _systemStatusTitle = "System Status";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private DateTime _lastUpdated = DateTime.Now;

        public DashboardViewModel(ConnectionManager connectionManager, PowerShellManager powerShellManager, AppConfig appConfig)
        {
            _connectionManager = connectionManager;
            _powerShellManager = powerShellManager;
            _appConfig = appConfig;

            WelcomeMessage = $"Welcome back to {appConfig.ApplicationName}! Here's your migration overview.";

            LoadSampleData();
            _ = RefreshDataAsync(); // Fire and forget initial load
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                SystemStatusMessage = "Refreshing dashboard data...";

                // Simulate API calls to get real data
                await UpdateConnectionStatusAsync();
                await UpdateMigrationStatisticsAsync();
                await UpdateSystemHealthAsync();

                LastUpdated = DateTime.Now;
                SystemStatusMessage = $"All systems operational. Last updated: {LastUpdated:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                SystemStatusMessage = $"Error refreshing data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void NewMigration()
        {
            // In a real implementation, this would navigate to the migration page
            SystemStatusMessage = "Navigate to Migration page to start a new migration";
        }

        [RelayCommand]
        private void TestConnections()
        {
            _ = TestConnectionsAsync();
        }

        [RelayCommand]
        private void BackupConfig()
        {
            try
            {
                // In a real implementation, this would backup configuration
                SystemStatusMessage = "Configuration backup completed successfully";
            }
            catch (Exception ex)
            {
                SystemStatusMessage = $"Backup failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ViewLogs()
        {
            // In a real implementation, this would navigate to the logs page
            SystemStatusMessage = "Navigate to Logs page to view detailed logs";
        }

        [RelayCommand]
        private void ExportReport()
        {
            try
            {
                var fileName = $"Migration_Report_{DateTime.Now:yyyy-MM-dd_HH-mm}.html";
                GenerateMigrationReport(fileName);
                SystemStatusMessage = $"Report exported to {fileName}";
            }
            catch (Exception ex)
            {
                SystemStatusMessage = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ViewAllActive()
        {
            // In a real implementation, this would open a detailed active migrations view
            SystemStatusMessage = "Showing all active migrations";
        }

        [RelayCommand]
        private void ViewMigrationHistory()
        {
            // In a real implementation, this would navigate to a migration history page
            SystemStatusMessage = "Navigate to view complete migration history";
        }

        private async Task UpdateConnectionStatusAsync()
        {
            // Simulate checking vCenter connections
            await Task.Delay(500);

            // In a real implementation, this would use PowerShell to test connections
            var profiles = _connectionManager.GetAllProfiles();

            // Update source connection (mock)
            SourceIsConnected = true;
            SourceConnectionStatus = SourceIsConnected ? "Connected" : "Disconnected";

            // Update destination connection (mock)
            DestinationIsConnected = true;
            DestinationConnectionStatus = DestinationIsConnected ? "Connected" : "Disconnected";
        }

        private async Task UpdateMigrationStatisticsAsync()
        {
            // Simulate getting migration statistics
            await Task.Delay(300);

            // In a real implementation, this would query migration history
            // For now, just vary the numbers slightly to show updates
            var random = new Random();
            TotalMigrations = 24 + random.Next(-2, 3);
            SuccessfulMigrations = TotalMigrations - FailedMigrations - InProgressMigrations;
        }

        private async Task UpdateSystemHealthAsync()
        {
            // Simulate system health checks
            await Task.Delay(200);

            // Update memory usage (simulate fluctuation)
            var random = new Random();
            MemoryUsagePercentage = 30 + random.NextDouble() * 10;
            var usedMB = (int)(1024 * MemoryUsagePercentage / 100);
            MemoryUsageText = $"{usedMB} MB / 1024 MB";

            // Update PowerCLI status
            PowerCliIsHealthy = true;
            PowerCliStatus = PowerCliIsHealthy ? "Connected" : "Disconnected";
        }

        private async Task TestConnectionsAsync()
        {
            try
            {
                IsLoading = true;
                SystemStatusMessage = "Testing vCenter connections...";

                // Simulate connection testing
                await Task.Delay(2000);

                await UpdateConnectionStatusAsync();
                SystemStatusMessage = "Connection test completed successfully";
            }
            catch (Exception ex)
            {
                SystemStatusMessage = $"Connection test failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GenerateMigrationReport(string fileName)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Migration Report - {DateTime.Now:yyyy-MM-dd}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
        .stats {{ display: flex; justify-content: space-around; margin: 20px 0; }}
        .stat {{ text-align: center; padding: 10px; background: #f8f9fa; border-radius: 5px; }}
        .connections {{ margin: 20px 0; }}
        .recent {{ margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>vCenter Migration Report</h1>
        <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    
    <div class='stats'>
        <div class='stat'>
            <h3>{TotalMigrations}</h3>
            <p>Total Migrations</p>
        </div>
        <div class='stat'>
            <h3>{SuccessfulMigrations}</h3>
            <p>Successful</p>
        </div>
        <div class='stat'>
            <h3>{FailedMigrations}</h3>
            <p>Failed</p>
        </div>
        <div class='stat'>
            <h3>{InProgressMigrations}</h3>
            <p>In Progress</p>
        </div>
    </div>
    
    <div class='connections'>
        <h2>vCenter Connections</h2>
        <p><strong>Source:</strong> {SourceVcenterName} - {SourceConnectionStatus}</p>
        <p><strong>Destination:</strong> {DestinationVcenterName} - {DestinationConnectionStatus}</p>
    </div>
    
    <div class='recent'>
        <h2>Recent Migrations</h2>
        {string.Join("", RecentMigrations.Select(m => $"<p>{m.VmName} - {m.Status} ({m.TimeAgo})</p>"))}
    </div>
</body>
</html>";

            System.IO.File.WriteAllText(fileName, html);
        }

        private void LoadSampleData()
        {
            // Load active migrations
            ActiveMigrations.Clear();
            ActiveMigrations.Add(new ActiveMigration
            {
                VmName = "WebServer03",
                MigrationType = "vMotion",
                Progress = 65,
                Status = "Migrating storage (65%)",
                TimeRemaining = "2m 15s remaining"
            });

            // Load recent migrations
            RecentMigrations.Clear();
            RecentMigrations.Add(new RecentMigration
            {
                VmName = "DatabaseServer01",
                Status = "Success",
                TimeAgo = "Completed 15 minutes ago",
                IsSuccess = true
            });
            RecentMigrations.Add(new RecentMigration
            {
                VmName = "WebServer02",
                Status = "Success",
                TimeAgo = "Completed 1 hour ago",
                IsSuccess = true
            });
            RecentMigrations.Add(new RecentMigration
            {
                VmName = "FileServer01",
                Status = "Failed",
                TimeAgo = "Failed 2 hours ago",
                IsSuccess = false
            });
        }
    }
}

