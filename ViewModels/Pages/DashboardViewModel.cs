using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ConnectionManager _connectionManager;
        private readonly BackupManager _backupManager;
        private readonly ILogger<DashboardViewModel> _logger;

        // Connection Status Properties
        [ObservableProperty]
        private string _sourceVCenterAddress = "Not Connected";

        [ObservableProperty]
        private string _sourceVCenterStatus = "Disconnected";

        [ObservableProperty]
        private string _sourceVCenterStatusColor = "Red";

        [ObservableProperty]
        private string _destinationVCenterAddress = "Not Connected";

        [ObservableProperty]
        private string _destinationVCenterStatus = "Disconnected";

        [ObservableProperty]
        private string _destinationVCenterStatusColor = "Red";

        // VM/Host/Cluster counts
        [ObservableProperty]
        private int _sourceVMCount = 0;

        [ObservableProperty]
        private int _sourceHostCount = 0;

        [ObservableProperty]
        private int _sourceClusterCount = 0;

        [ObservableProperty]
        private int _destinationVMCount = 0;

        [ObservableProperty]
        private int _destinationHostCount = 0;

        [ObservableProperty]
        private int _destinationClusterCount = 0;

        // Migration Statistics
        [ObservableProperty]
        private int _totalMigrations = 0;

        [ObservableProperty]
        private int _successfulMigrations = 0;

        [ObservableProperty]
        private int _failedMigrations = 0;

        [ObservableProperty]
        private int _inProgressMigrations = 0;

        // Collections
        [ObservableProperty]
        private ObservableCollection<ActiveMigration> _activeMigrations = new();

        [ObservableProperty]
        private ObservableCollection<RecentMigration> _recentMigrations = new();

        // System Health
        [ObservableProperty]
        private string _powerCLIStatus = "Checking...";

        [ObservableProperty]
        private string _powerCLIVersion = "Unknown";

        [ObservableProperty]
        private double _memoryUsage = 0;

        [ObservableProperty]
        private string _memoryUsageText = "0 MB / 0 MB";

        [ObservableProperty]
        private string _logFileSize = "0 MB";

        [ObservableProperty]
        private string _logFileStatus = "OK";

        // Status
        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _lastUpdated = "Never";

        public DashboardViewModel(
            ConnectionManager connectionManager,
            BackupManager backupManager,
            ILogger<DashboardViewModel> logger)
        {
            _connectionManager = connectionManager;
            _backupManager = backupManager;
            _logger = logger;

            // Subscribe to connection status changes
            _connectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading dashboard data...";

                await UpdateConnectionStatusAsync();
                await LoadMigrationStatisticsAsync();
                await LoadSystemHealthAsync();
                await LoadRecentActivityAsync();

                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
                StatusMessage = "Dashboard updated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard data");
                StatusMessage = $"Error loading dashboard: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task TestConnectionsAsync()
        {
            try
            {
                StatusMessage = "Testing connections...";

                if (_connectionManager.CurrentConnection != null)
                {
                    var result = await _connectionManager.ValidateCurrentConnectionAsync();
                    StatusMessage = result ? "Connection test successful" : "Connection test failed";
                }
                else
                {
                    StatusMessage = "No active connection to test";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                StatusMessage = $"Connection test error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task BackupConfigAsync()
        {
            try
            {
                StatusMessage = "Starting backup...";

                var result = await _backupManager.PerformVMBackupAsync();
                StatusMessage = result.IsSuccessful
                    ? "Backup completed successfully"
                    : $"Backup failed: {result.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup failed");
                StatusMessage = $"Backup error: {ex.Message}";
            }
        }

        private async Task UpdateConnectionStatusAsync()
        {
            try
            {
                if (_connectionManager.CurrentConnection != null)
                {
                    var isValid = await _connectionManager.ValidateCurrentConnectionAsync();

                    if (isValid)
                    {
                        SourceVCenterAddress = _connectionManager.CurrentConnection.ServerAddress;
                        SourceVCenterStatus = "Connected";
                        SourceVCenterStatusColor = "LimeGreen";

                        // Get VM inventory to update counts
                        var inventory = await _backupManager.GetVMInventoryAsync();
                        SourceVMCount = inventory.Count;

                        // For now, set placeholder values for hosts and clusters
                        // These would come from additional PowerShell calls
                        SourceHostCount = 0; // Would be populated from Get-VMHost
                        SourceClusterCount = 0; // Would be populated from Get-Cluster
                    }
                    else
                    {
                        ResetConnectionStatus();
                    }
                }
                else
                {
                    ResetConnectionStatus();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update connection status");
                ResetConnectionStatus();
            }
        }

        private void ResetConnectionStatus()
        {
            SourceVCenterAddress = "Not Connected";
            SourceVCenterStatus = "Disconnected";
            SourceVCenterStatusColor = "Red";
            SourceVMCount = 0;
            SourceHostCount = 0;
            SourceClusterCount = 0;
        }

        private async Task LoadMigrationStatisticsAsync()
        {
            try
            {
                // For now, these would come from a migration history service
                // This is placeholder logic - you'd implement actual migration tracking
                TotalMigrations = 0;
                SuccessfulMigrations = 0;
                FailedMigrations = 0;
                InProgressMigrations = 0;

                // Clear collections and add real data when available
                ActiveMigrations.Clear();
                RecentMigrations.Clear();

                // TODO: Implement actual migration tracking service
                // var migrationHistory = await _migrationService.GetMigrationHistoryAsync();
                // var activeMigrations = await _migrationService.GetActiveMigrationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load migration statistics");
            }
        }

        private async Task LoadSystemHealthAsync()
        {
            try
            {
                // Check PowerCLI status
                PowerCLIStatus = "Connected"; // This would come from actual PowerCLI check
                PowerCLIVersion = "13.2.1"; // This would come from Get-Module VMware.PowerCLI

                // Memory usage
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var memoryMB = currentProcess.WorkingSet64 / 1024 / 1024;
                MemoryUsage = Math.Min((double)memoryMB / 1024 * 100, 100); // Percentage assuming 1GB max
                MemoryUsageText = $"{memoryMB} MB / 1024 MB";

                // Log file size
                var logPath = _backupManager.DefaultLogPath;
                if (System.IO.Directory.Exists(logPath))
                {
                    var logFiles = System.IO.Directory.GetFiles(logPath, "*.log");
                    long totalSize = 0;
                    foreach (var file in logFiles)
                    {
                        totalSize += new System.IO.FileInfo(file).Length;
                    }

                    var sizeMB = totalSize / 1024 / 1024;
                    LogFileSize = $"{sizeMB} MB";
                    LogFileStatus = sizeMB > 50 ? "Archive recommended" : "OK";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load system health");
            }
        }

        private async Task LoadRecentActivityAsync()
        {
            try
            {
                // Load recent backup activity
                var backupItems = _backupManager.BackupItems;
                RecentMigrations.Clear();

                // Convert backup items to recent migrations for display
                foreach (var backup in backupItems.Take(3))
                {
                    RecentMigrations.Add(new RecentMigration
                    {
                        VmName = backup.Name,
                        SourceCluster = "Backup",
                        DestinationCluster = "Archive",
                        MigrationDate = backup.CreatedDate
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load recent activity");
            }
        }

        private void OnConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            // Update UI when connection status changes
            _ = Task.Run(async () => await UpdateConnectionStatusAsync());
        }

        public void Dispose()
        {
            _connectionManager.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }
    }
}
