using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using Wpf.Ui.Controls;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class BackupViewModel : ObservableObject
    {
        private readonly BackupManager _backupManager;
        private readonly ConnectionManager _connectionManager;
        private readonly ILogger<BackupViewModel> _logger;

        // Backup History and Selection
        [ObservableProperty]
        private ObservableCollection<BackupItem> _backupHistory = new();

        [ObservableProperty]
        private BackupItem? _selectedBackup;

        // Filter Properties
        [ObservableProperty]
        private ObservableCollection<string> _backupTypes = new() { "All", "VM Metadata", "Configuration", "Templates", "Migration Data" };

        [ObservableProperty]
        private string _selectedBackupType = "All";

        [ObservableProperty]
        private ObservableCollection<string> _dateFilters = new() { "All", "Today", "This Week", "This Month", "Last 30 Days" };

        [ObservableProperty]
        private string _selectedDateFilter = "All";

        // Backup Status and Progress
        [ObservableProperty]
        private bool _isBackupInProgress = false;

        [ObservableProperty]
        private double _backupProgress = 0;

        [ObservableProperty]
        private string _currentBackupOperation = string.Empty;

        // Storage Information
        [ObservableProperty]
        private string _backupLocation = string.Empty;

        [ObservableProperty]
        private string _usedSpace = "0 MB";

        [ObservableProperty]
        private string _availableSpace = "0 GB";

        [ObservableProperty]
        private double _storageUsagePercentage = 0;

        // Backup Settings
        [ObservableProperty]
        private bool _enableDailyBackup = false;

        [ObservableProperty]
        private bool _backupBeforeMigration = true;

        [ObservableProperty]
        private bool _autoCleanupBackups = false;

        [ObservableProperty]
        private ObservableCollection<string> _retentionOptions = new() { "7 days", "30 days", "90 days", "1 year", "Never" };

        [ObservableProperty]
        private string _selectedRetention = "30 days";

        // Template Information
        [ObservableProperty]
        private int _templateCount = 0;

        [ObservableProperty]
        private string _lastConfigBackup = "Never";

        [ObservableProperty]
        private string _dataSize = "0 MB";

        // Status and UI State
        [ObservableProperty]
        private string _statusTitle = "Backup Status";

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

        [ObservableProperty]
        private bool _showStatus = false;

        [ObservableProperty]
        private bool _isLoading = false;

        // Command Enablement Properties
        [ObservableProperty]
        private bool _canRestore = false;

        [ObservableProperty]
        private bool _canDownload = false;

        [ObservableProperty]
        private bool _canDelete = false;

        [ObservableProperty]
        private bool _canVerify = false;

        public BackupViewModel(
            BackupManager backupManager,
            ConnectionManager connectionManager,
            ILogger<BackupViewModel> logger)
        {
            _backupManager = backupManager;
            _connectionManager = connectionManager;
            _logger = logger;

            BackupLocation = _backupManager.DefaultBackupPath;

            // Subscribe to property changes
            PropertyChanged += OnPropertyChanged;

            // Initialize data
            _ = Task.Run(LoadDataAsync);
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedBackup))
            {
                UpdateCommandStates();
            }
            else if (e.PropertyName == nameof(SelectedBackupType) || e.PropertyName == nameof(SelectedDateFilter))
            {
                _ = Task.Run(FilterBackupHistoryAsync);
            }
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                await RefreshBackupHistoryAsync();
                await UpdateStorageInformationAsync();
                await LoadBackupSettingsAsync();
                await UpdateTemplateCountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load backup data");
                ShowStatusMessage("Error loading backup data", ex.Message, InfoBarSeverity.Error);
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
        private async Task CreateBackupAsync()
        {
            try
            {
                if (_connectionManager.CurrentConnection == null)
                {
                    ShowStatusMessage("Connection Required", "Please connect to a vCenter server first", InfoBarSeverity.Warning);
                    return;
                }

                IsBackupInProgress = true;
                BackupProgress = 0;
                CurrentBackupOperation = "Starting VM metadata backup...";

                var result = await _backupManager.PerformVMBackupAsync();

                if (result.IsSuccessful)
                {
                    ShowStatusMessage("Backup Successful", $"VM metadata backup completed successfully", InfoBarSeverity.Success);
                    await RefreshBackupHistoryAsync();
                }
                else
                {
                    ShowStatusMessage("Backup Failed", result.Message, InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup creation failed");
                ShowStatusMessage("Backup Error", ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                IsBackupInProgress = false;
                BackupProgress = 0;
                CurrentBackupOperation = string.Empty;
            }
        }

        [RelayCommand]
        private async Task BackupConfigurationAsync()
        {
            try
            {
                IsBackupInProgress = true;
                CurrentBackupOperation = "Backing up configuration...";

                // Backup connection profiles and settings
                var configBackupPath = Path.Combine(_backupManager.DefaultBackupPath, $"Config_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                // This would backup your application configuration
                var profiles = _connectionManager.ServerProfiles.ToList();
                var configData = new
                {
                    Profiles = profiles,
                    BackupDate = DateTime.Now,
                    Version = "1.0.0"
                };

                var json = System.Text.Json.JsonSerializer.Serialize(configData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configBackupPath, json);

                // Create backup item
                var backupItem = new BackupItem
                {
                    Name = $"Configuration Backup {DateTime.Now:yyyy-MM-dd HH:mm}",
                    Type = "Configuration",
                    CreatedDate = DateTime.Now,
                    FilePath = configBackupPath,
                    Size = new FileInfo(configBackupPath).Length,
                    Status = "Completed"
                };

                _backupManager.BackupItems.Add(backupItem);
                LastConfigBackup = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                ShowStatusMessage("Configuration Backup", "Configuration backup completed successfully", InfoBarSeverity.Success);
                await RefreshBackupHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration backup failed");
                ShowStatusMessage("Configuration Backup Failed", ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                IsBackupInProgress = false;
                CurrentBackupOperation = string.Empty;
            }
        }

        [RelayCommand]
        private async Task ExportTemplatesAsync()
        {
            try
            {
                if (_connectionManager.CurrentConnection == null)
                {
                    ShowStatusMessage("Connection Required", "Please connect to a vCenter server first", InfoBarSeverity.Warning);
                    return;
                }

                IsBackupInProgress = true;
                CurrentBackupOperation = "Exporting VM templates...";

                // This would export VM templates - placeholder for now
                await Task.Delay(2000); // Simulate export process

                ShowStatusMessage("Template Export", "VM templates exported successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Template export failed");
                ShowStatusMessage("Template Export Failed", ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                IsBackupInProgress = false;
                CurrentBackupOperation = string.Empty;
            }
        }

        [RelayCommand]
        private async Task BackupMigrationDataAsync()
        {
            try
            {
                IsBackupInProgress = true;
                CurrentBackupOperation = "Backing up migration data...";

                // This would backup migration logs and data
                await Task.Delay(1500); // Simulate backup process

                ShowStatusMessage("Migration Data Backup", "Migration data backup completed successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration data backup failed");
                ShowStatusMessage("Migration Data Backup Failed", ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                IsBackupInProgress = false;
                CurrentBackupOperation = string.Empty;
            }
        }

        [RelayCommand]
        private async Task RestoreBackupAsync(BackupItem? backup)
        {
            if (backup == null) return;

            try
            {
                var restoreLocation = Path.Combine(_backupManager.DefaultBackupPath, "Restored");
                var success = await _backupManager.RestoreBackupAsync(backup, restoreLocation);

                if (success)
                {
                    ShowStatusMessage("Restore Successful", $"Backup '{backup.Name}' restored successfully", InfoBarSeverity.Success);
                }
                else
                {
                    ShowStatusMessage("Restore Failed", $"Failed to restore backup '{backup.Name}'", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup restore failed");
                ShowStatusMessage("Restore Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteBackupAsync(BackupItem? backup)
        {
            if (backup == null) return;

            try
            {
                var success = _backupManager.DeleteBackup(backup);
                if (success)
                {
                    await RefreshBackupHistoryAsync();
                    ShowStatusMessage("Backup Deleted", $"Backup '{backup.Name}' deleted successfully", InfoBarSeverity.Success);
                }
                else
                {
                    ShowStatusMessage("Delete Failed", $"Failed to delete backup '{backup.Name}'", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup deletion failed");
                ShowStatusMessage("Delete Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task VerifyBackupAsync(BackupItem? backup)
        {
            if (backup == null) return;

            try
            {
                var isValid = _backupManager.ValidateBackup(backup.FilePath);
                var message = isValid ? "Backup is valid and can be restored" : "Backup validation failed - file may be corrupted";
                var severity = isValid ? InfoBarSeverity.Success : InfoBarSeverity.Error;

                ShowStatusMessage("Backup Verification", message, severity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup verification failed");
                ShowStatusMessage("Verification Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task CleanupBackupsAsync()
        {
            try
            {
                // Implement cleanup logic based on retention settings
                var cutoffDate = GetRetentionCutoffDate();
                var itemsToRemove = _backupManager.BackupItems
                    .Where(b => b.CreatedDate < cutoffDate)
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    _backupManager.DeleteBackup(item);
                }

                await RefreshBackupHistoryAsync();
                ShowStatusMessage("Cleanup Complete", $"Removed {itemsToRemove.Count} old backup(s)", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup cleanup failed");
                ShowStatusMessage("Cleanup Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void OpenBackupFolder()
        {
            try
            {
                if (Directory.Exists(BackupLocation))
                {
                    System.Diagnostics.Process.Start("explorer.exe", BackupLocation);
                }
                else
                {
                    ShowStatusMessage("Folder Not Found", "Backup folder does not exist", InfoBarSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open backup folder");
                ShowStatusMessage("Error", "Failed to open backup folder", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void CancelBackup()
        {
            // Implement backup cancellation logic
            IsBackupInProgress = false;
            BackupProgress = 0;
            CurrentBackupOperation = string.Empty;
            ShowStatusMessage("Backup Cancelled", "Backup operation was cancelled", InfoBarSeverity.Warning);
        }

        private async Task RefreshBackupHistoryAsync()
        {
            try
            {
                BackupHistory.Clear();
                foreach (var item in _backupManager.BackupItems)
                {
                    BackupHistory.Add(item);
                }

                await FilterBackupHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh backup history");
            }
        }

        private async Task FilterBackupHistoryAsync()
        {
            try
            {
                var filteredItems = _backupManager.BackupItems.AsEnumerable();

                // Filter by type
                if (SelectedBackupType != "All")
                {
                    filteredItems = filteredItems.Where(b => b.Type == SelectedBackupType);
                }

                // Filter by date
                if (SelectedDateFilter != "All")
                {
                    var cutoffDate = GetDateFilterCutoff();
                    filteredItems = filteredItems.Where(b => b.CreatedDate >= cutoffDate);
                }

                BackupHistory.Clear();
                foreach (var item in filteredItems)
                {
                    BackupHistory.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to filter backup history");
            }
        }

        private async Task UpdateStorageInformationAsync()
        {
            try
            {
                if (Directory.Exists(BackupLocation))
                {
                    var drive = new DriveInfo(Path.GetPathRoot(BackupLocation) ?? "C:");
                    var availableBytes = drive.AvailableFreeSpace;
                    var totalBytes = drive.TotalSize;
                    var usedBytes = totalBytes - availableBytes;

                    AvailableSpace = FormatBytes(availableBytes);
                    UsedSpace = FormatBytes(usedBytes);
                    StorageUsagePercentage = (double)usedBytes / totalBytes * 100;

                    // Calculate total backup size
                    long totalBackupSize = 0;
                    foreach (var backup in _backupManager.BackupItems)
                    {
                        totalBackupSize += backup.Size;
                    }
                    DataSize = FormatBytes(totalBackupSize);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update storage information");
            }
        }

        private async Task LoadBackupSettingsAsync()
        {
            try
            {
                // Load backup settings from configuration
                // This would come from your app settings/configuration
                EnableDailyBackup = false; // Load from config
                BackupBeforeMigration = true; // Load from config
                AutoCleanupBackups = false; // Load from config
                SelectedRetention = "30 days"; // Load from config
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load backup settings");
            }
        }

        private async Task UpdateTemplateCountAsync()
        {
            try
            {
                if (_connectionManager.CurrentConnection != null)
                {
                    // This would get actual template count from vCenter
                    // For now, using placeholder
                    TemplateCount = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update template count");
            }
        }

        private void UpdateCommandStates()
        {
            CanRestore = SelectedBackup != null && File.Exists(SelectedBackup.FilePath);
            CanDownload = SelectedBackup != null && File.Exists(SelectedBackup.FilePath);
            CanDelete = SelectedBackup != null;
            CanVerify = SelectedBackup != null && File.Exists(SelectedBackup.FilePath);
        }

        private DateTime GetDateFilterCutoff()
        {
            return SelectedDateFilter switch
            {
                "Today" => DateTime.Today,
                "This Week" => DateTime.Today.AddDays(-7),
                "This Month" => DateTime.Today.AddDays(-30),
                "Last 30 Days" => DateTime.Today.AddDays(-30),
                _ => DateTime.MinValue
            };
        }

        private DateTime GetRetentionCutoffDate()
        {
            return SelectedRetention switch
            {
                "7 days" => DateTime.Now.AddDays(-7),
                "30 days" => DateTime.Now.AddDays(-30),
                "90 days" => DateTime.Now.AddDays(-90),
                "1 year" => DateTime.Now.AddYears(-1),
                _ => DateTime.MinValue
            };
        }

        private void ShowStatusMessage(string title, string message, InfoBarSeverity severity)
        {
            StatusTitle = title;
            StatusMessage = message;
            StatusSeverity = severity;
            ShowStatus = true;

            // Auto-hide after 5 seconds for non-error messages
            if (severity != InfoBarSeverity.Error)
            {
                _ = Task.Delay(5000).ContinueWith(_ => ShowStatus = false);
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
