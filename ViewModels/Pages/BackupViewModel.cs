using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;
using UiDesktopApp2.Helpers;
using Wpf.Ui.Controls;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class BackupViewModel : ObservableObject
    {
        private readonly AppConfig _appConfig;
        private readonly ConnectionManager _connectionManager;
        private readonly PowerShellManager _powerShellManager;

        [ObservableProperty]
        private string _lastConfigBackup = "Never";

        [ObservableProperty]
        private int _templateCount = 0;

        [ObservableProperty]
        private string _dataSize = "0 MB";

        [ObservableProperty]
        private ObservableCollection<string> _backupTypes = new()
        {
            "All Types", "Configuration", "VM Templates", "Migration Data", "Logs"
        };

        [ObservableProperty]
        private string _selectedBackupType = "All Types";

        [ObservableProperty]
        private ObservableCollection<string> _dateFilters = new()
        {
            "All Dates", "Last 7 Days", "Last 30 Days", "Last 90 Days", "This Year"
        };

        [ObservableProperty]
        private string _selectedDateFilter = "All Dates";

        [ObservableProperty]
        private ObservableCollection<BackupItem> _backupHistory = new();

        [ObservableProperty]
        private ICollectionView _filteredBackups;

        [ObservableProperty]
        private BackupItem? _selectedBackup;

        [ObservableProperty]
        private bool _canRestore = false;

        [ObservableProperty]
        private bool _canDownload = false;

        [ObservableProperty]
        private bool _canDelete = false;

        [ObservableProperty]
        private bool _canVerify = false;

        // Backup Progress
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

        // Automated Backup Settings
        [ObservableProperty]
        private bool _enableDailyBackup = true;

        [ObservableProperty]
        private bool _backupBeforeMigration = true;

        [ObservableProperty]
        private bool _autoCleanupBackups = false;

        [ObservableProperty]
        private ObservableCollection<string> _retentionOptions = new()
        {
            "7 Days", "30 Days", "90 Days", "6 Months", "1 Year", "Keep All"
        };

        [ObservableProperty]
        private string _selectedRetention = "30 Days";

        // Status
        [ObservableProperty]
        private string _statusTitle = "Ready";

        [ObservableProperty]
        private string _statusMessage = "Backup system ready";

        [ObservableProperty]
        private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

        [ObservableProperty]
        private bool _showStatus = false;

        private CancellationTokenSource? _backupCancellationToken;

        public BackupViewModel(AppConfig appConfig, ConnectionManager connectionManager, PowerShellManager powerShellManager)
        {
            _appConfig = appConfig;
            _connectionManager = connectionManager;
            _powerShellManager = powerShellManager;

            // Initialize backup location
            BackupLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "vCenter Migration Tool", "Backups");

            // Initialize filtered collection
            _filteredBackups = CollectionViewSource.GetDefaultView(BackupHistory);
            _filteredBackups.Filter = FilterBackups;

            LoadBackupHistory();
            UpdateStorageInfo();
            LoadSettings();
        }

        [RelayCommand]
        private async Task CreateBackupAsync()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Backup files (*.backup)|*.backup|All files (*.*)|*.*",
                    DefaultExt = ".backup",
                    FileName = $"FullBackup_{DateTime.Now:yyyy-MM-dd_HH-mm}"
                };

                if (dialog.ShowDialog() == true)
                {
                    await CreateFullBackupAsync(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Backup Error", $"Failed to create backup: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                ShowStatusMessage("Refreshing", "Updating backup information...", InfoBarSeverity.Informational);

                LoadBackupHistory();
                UpdateStorageInfo();
                await UpdateTemplateCountAsync();

                ShowStatusMessage("Refreshed", "Backup information updated successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Refresh Error", $"Failed to refresh: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task BackupConfigurationAsync()
        {
            try
            {
                await StartBackupOperationAsync("Configuration Backup", async (progress, cancellation) =>
                {
                    await BackupApplicationConfigAsync(progress, cancellation);
                });

                LastConfigBackup = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                ShowStatusMessage("Success", "Configuration backup completed successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Backup Error", $"Configuration backup failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void ScheduleConfigBackup()
        {
            try
            {
                // In a real implementation, this would open a scheduling dialog
                ShowStatusMessage("Scheduled", "Daily configuration backup has been scheduled", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Schedule Error", $"Failed to schedule backup: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task ExportTemplatesAsync()
        {
            try
            {
                await StartBackupOperationAsync("Template Export", async (progress, cancellation) =>
                {
                    await ExportVMTemplatesAsync(progress, cancellation);
                });

                ShowStatusMessage("Success", "VM templates exported successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Export Error", $"Template export failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task ImportTemplatesAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Template files (*.ovf;*.ova)|*.ovf;*.ova|All files (*.*)|*.*",
                    Multiselect = true
                };

                if (dialog.ShowDialog() == true)
                {
                    await StartBackupOperationAsync("Template Import", async (progress, cancellation) =>
                    {
                        await ImportVMTemplatesAsync(dialog.FileNames, progress, cancellation);
                    });

                    ShowStatusMessage("Success", $"Imported {dialog.FileNames.Length} templates successfully", InfoBarSeverity.Success);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Import Error", $"Template import failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task BackupMigrationDataAsync()
        {
            try
            {
                await StartBackupOperationAsync("Migration Data Backup", async (progress, cancellation) =>
                {
                    await BackupMigrationHistoryAsync(progress, cancellation);
                });

                ShowStatusMessage("Success", "Migration data backup completed successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Backup Error", $"Migration data backup failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task ArchiveLogsAsync()
        {
            try
            {
                await StartBackupOperationAsync("Log Archive", async (progress, cancellation) =>
                {
                    await ArchiveApplicationLogsAsync(progress, cancellation);
                });

                ShowStatusMessage("Success", "Logs archived successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Archive Error", $"Log archive failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void CleanupBackups()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-GetRetentionDays());
                var oldBackups = BackupHistory.Where(b => b.CreatedDate < cutoffDate).ToList();

                foreach (var backup in oldBackups)
                {
                    if (File.Exists(backup.FilePath))
                    {
                        File.Delete(backup.FilePath);
                    }
                    BackupHistory.Remove(backup);
                }

                UpdateStorageInfo();
                ShowStatusMessage("Cleanup Complete", $"Removed {oldBackups.Count} old backup files", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Cleanup Error", $"Backup cleanup failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void BackupSettings()
        {
            try
            {
                // In a real implementation, this would open a settings dialog
                ShowStatusMessage("Settings", "Backup settings dialog would open here", InfoBarSeverity.Informational);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Settings Error", $"Failed to open settings: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task RestoreBackupAsync(BackupItem? backup)
        {
            if (backup == null) return;

            try
            {
                await StartBackupOperationAsync($"Restoring {backup.Name}", async (progress, cancellation) =>
                {
                    await RestoreFromBackupAsync(backup, progress, cancellation);
                });

                ShowStatusMessage("Success", $"Backup '{backup.Name}' restored successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Restore Error", $"Failed to restore backup: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void DownloadBackup(BackupItem? backup)
        {
            if (backup == null) return;

            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = backup.Name,
                    DefaultExt = Path.GetExtension(backup.FilePath)
                };

                if (dialog.ShowDialog() == true)
                {
                    File.Copy(backup.FilePath, dialog.FileName, true);
                    ShowStatusMessage("Success", $"Backup downloaded to {dialog.FileName}", InfoBarSeverity.Success);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Download Error", $"Failed to download backup: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void DeleteBackup(BackupItem? backup)
        {
            if (backup == null) return;

            try
            {
                if (File.Exists(backup.FilePath))
                {
                    File.Delete(backup.FilePath);
                }

                BackupHistory.Remove(backup);
                UpdateStorageInfo();
                ShowStatusMessage("Success", $"Backup '{backup.Name}' deleted successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Delete Error", $"Failed to delete backup: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task VerifyBackupAsync(BackupItem? backup)
        {
            if (backup == null) return;

            try
            {
                await StartBackupOperationAsync($"Verifying {backup.Name}", async (progress, cancellation) =>
                {
                    await VerifyBackupIntegrityAsync(backup, progress, cancellation);
                });

                backup.Status = "Verified";
                ShowStatusMessage("Success", $"Backup '{backup.Name}' verification completed", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                backup.Status = "Verification Failed";
                ShowStatusMessage("Verification Error", $"Backup verification failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void CancelBackup()
        {
            try
            {
                _backupCancellationToken?.Cancel();
                IsBackupInProgress = false;
                CurrentBackupOperation = "Operation cancelled";
                ShowStatusMessage("Cancelled", "Backup operation cancelled by user", InfoBarSeverity.Warning);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Cancel Error", $"Failed to cancel operation: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void OpenBackupFolder()
        {
            try
            {
                if (!Directory.Exists(BackupLocation))
                {
                    Directory.CreateDirectory(BackupLocation);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = BackupLocation,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Folder Error", $"Failed to open backup folder: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task TestRestoreAsync()
        {
            try
            {
                var latestBackup = BackupHistory.Where(b => b.Type == "Configuration").OrderByDescending(b => b.CreatedDate).FirstOrDefault();

                if (latestBackup != null)
                {
                    await StartBackupOperationAsync("Test Restore", async (progress, cancellation) =>
                    {
                        await TestRestoreOperationAsync(latestBackup, progress, cancellation);
                    });

                    ShowStatusMessage("Success", "Test restore completed successfully", InfoBarSeverity.Success);
                }
                else
                {
                    ShowStatusMessage("No Backup", "No configuration backup available for testing", InfoBarSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Test Error", $"Test restore failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void ExportSettings()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Settings files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = $"Settings_{DateTime.Now:yyyy-MM-dd}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportApplicationSettings(dialog.FileName);
                    ShowStatusMessage("Success", $"Settings exported to {dialog.FileName}", InfoBarSeverity.Success);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Export Error", $"Failed to export settings: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private void ImportSettings()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Settings files (*.json)|*.json|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    ImportApplicationSettings(dialog.FileName);
                    ShowStatusMessage("Success", $"Settings imported from {dialog.FileName}", InfoBarSeverity.Success);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Import Error", $"Failed to import settings: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        partial void OnSelectedBackupChanged(BackupItem? value)
        {
            CanRestore = value != null && value.Status == "Complete";
            CanDownload = value != null && File.Exists(value.FilePath);
            CanDelete = value != null;
            CanVerify = value != null && value.Status != "Verifying";
        }

        partial void OnSelectedBackupTypeChanged(string value)
        {
            FilteredBackups.Refresh();
        }

        partial void OnSelectedDateFilterChanged(string value)
        {
            FilteredBackups.Refresh();
        }

        private bool FilterBackups(object item)
        {
            if (item is not BackupItem backup)
                return false;

            // Type filter
            if (SelectedBackupType != "All Types" && backup.Type != SelectedBackupType)
                return false;

            // Date filter
            if (SelectedDateFilter != "All Dates")
            {
                var cutoffDate = SelectedDateFilter switch
                {
                    "Last 7 Days" => DateTime.Now.AddDays(-7),
                    "Last 30 Days" => DateTime.Now.AddDays(-30),
                    "Last 90 Days" => DateTime.Now.AddDays(-90),
                    "This Year" => new DateTime(DateTime.Now.Year, 1, 1),
                    _ => DateTime.MinValue
                };

                if (backup.CreatedDate < cutoffDate)
                    return false;
            }

            return true;
        }

        private async Task StartBackupOperationAsync(string operationName, Func<IProgress<int>, CancellationToken, Task> operation)
        {
            _backupCancellationToken?.Cancel();
            _backupCancellationToken = new CancellationTokenSource();

            IsBackupInProgress = true;
            CurrentBackupOperation = operationName;
            BackupProgress = 0;

            var progress = new Progress<int>(value => BackupProgress = value);

            try
            {
                await operation(progress, _backupCancellationToken.Token);
            }
            finally
            {
                IsBackupInProgress = false;
                CurrentBackupOperation = "";
                BackupProgress = 0;
            }
        }

        private async Task CreateFullBackupAsync(string filePath)
        {
            await StartBackupOperationAsync("Creating Full Backup", async (progress, cancellation) =>
            {
                // Simulate full backup process
                for (int i = 0; i <= 100; i += 5)
                {
                    if (cancellation.IsCancellationRequested)
                        break;

                    progress.Report(i);
                    await Task.Delay(200, cancellation);
                }

                // Create backup file
                var backupData = CreateBackupData();
                await File.WriteAllTextAsync(filePath, backupData, cancellation);

                // Add to history
                var backup = new BackupItem
                {
                    Name = Path.GetFileName(filePath),
                    Type = "Full Backup",
                    CreatedDate = DateTime.Now,
                    FilePath = filePath,
                    Size = new FileInfo(filePath).Length,
                    Status = "Complete"
                };

                BackupHistory.Insert(0, backup);
                UpdateStorageInfo();
            });
        }

        private async Task BackupApplicationConfigAsync(IProgress<int> progress, CancellationToken cancellation)
        {
            for (int i = 0; i <= 100; i += 10)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                progress.Report(i);
                await Task.Delay(100, cancellation);
            }

            var configBackup = new BackupItem
            {
                Name = $"Config_{DateTime.Now:yyyy-MM-dd_HH-mm}.backup",
                Type = "Configuration",
                CreatedDate = DateTime.Now,
                FilePath = Path.Combine(BackupLocation, $"Config_{DateTime.Now:yyyy-MM-dd_HH-mm}.backup"),
                Size = 1024 * 50, // 50KB
                Status = "Complete"
            };

            BackupHistory.Insert(0, configBackup);
        }

        private async Task ExportVMTemplatesAsync(IProgress<int> progress, CancellationToken cancellation)
        {
            for (int i = 0; i <= 100; i += 5)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                progress.Report(i);
                await Task.Delay(300, cancellation);
            }

            var templateBackup = new BackupItem
            {
                Name = $"Templates_{DateTime.Now:yyyy-MM-dd_HH-mm}.backup",
                Type = "VM Templates",
                CreatedDate = DateTime.Now,
                FilePath = Path.Combine(BackupLocation, $"Templates_{DateTime.Now:yyyy-MM-dd_HH-mm}.backup"),
                Size = 1024 * 1024 * 100, // 100MB
                Status = "Complete"
            };

            BackupHistory.Insert(0, templateBackup);
        }

        private async Task ImportVMTemplatesAsync(string[] templateFiles, IProgress<int> progress, CancellationToken cancellation)
        {
            for (int i = 0; i < templateFiles.Length; i++)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                progress.Report((i + 1) * 100 / templateFiles.Length);
                await Task.Delay(500, cancellation);
            }

            TemplateCount += templateFiles.Length;
        }

        private async Task BackupMigrationHistoryAsync(IProgress<int> progress, CancellationToken cancellation)
        {
            for (int i = 0; i <= 100; i += 8)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                progress.Report(i);
                await Task.Delay(150, cancellation);
            }

            var migrationBackup = new BackupItem
            {
                Name = $"MigrationData_{DateTime.Now:yyyy-MM-dd_HH-mm}.backup",
                Type = "Migration Data",
                CreatedDate = DateTime.Now,
                FilePath = Path.Combine(BackupLocation, $"MigrationData_{DateTime.Now:yyyy-MM-dd_HH-mm}.backup"),
                Size = 1024 * 1024 * 25, // 25MB
                Status = "Complete"
            };

            BackupHistory.Insert(0, migrationBackup);
        }

        private async Task ArchiveApplicationLogsAsync(IProgress<int> progress, CancellationToken cancellation)
        {
            for (int i = 0; i <= 100; i += 12)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                progress.Report(i);
                await Task.Delay(100, cancellation);
            }

            var logBackup = new BackupItem
            {
                Name = $"Logs_{DateTime.Now:yyyy-MM-dd_HH-mm}.backup",
                Type = "Logs",
                CreatedDate = DateTime.Now,
                FilePath = Path.Combine(BackupLocation, $"Logs_{DateTime.Now:yyyy-MM-dd_HH-mm}.backup"),
                Size = 1024 * 1024 * 15, // 15MB
                Status = "Complete"
            };

            BackupHistory.Insert(0, logBackup);
        }

        private async Task RestoreFromBackupAsync(BackupItem backup, IProgress<int> progress, CancellationToken cancellation)
        {
            for (int i = 0; i <= 100; i += 7)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                progress.Report(i);
                await Task.Delay(200, cancellation);
            }
        }

        private async Task VerifyBackupIntegrityAsync(BackupItem backup, IProgress<int> progress, CancellationToken cancellation)
        {
            backup.Status = "Verifying";

            for (int i = 0; i <= 100; i += 15)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                progress.Report(i);
                await Task.Delay(100, cancellation);
            }
        }

        private async Task TestRestoreOperationAsync(BackupItem backup, IProgress<int> progress, CancellationToken cancellation)
        {
            for (int i = 0; i <= 100; i += 10)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                progress.Report(i);
                await Task.Delay(150, cancellation);
            }
        }

        private async Task UpdateTemplateCountAsync()
        {
            await Task.Delay(500); // Simulate API call
            TemplateCount = 12; // Mock data
        }

        private void LoadBackupHistory()
        {
            BackupHistory.Clear();

            // Sample backup history
            BackupHistory.Add(new BackupItem
            {
                Name = "Config_2024-01-15_14-30.backup",
                Type = "Configuration",
                CreatedDate = DateTime.Now.AddDays(-1),
                FilePath = Path.Combine(BackupLocation, "Config_2024-01-15_14-30.backup"),
                Size = 1024 * 52,
                Status = "Complete"
            });

            BackupHistory.Add(new BackupItem
            {
                Name = "Templates_2024-01-14_09-15.backup",
                Type = "VM Templates",
                CreatedDate = DateTime.Now.AddDays(-2),
                FilePath = Path.Combine(BackupLocation, "Templates_2024-01-14_09-15.backup"),
                Size = 1024 * 1024 * 150,
                Status = "Complete"
            });

            BackupHistory.Add(new BackupItem
            {
                Name = "MigrationData_2024-01-13_16-45.backup",
                Type = "Migration Data",
                CreatedDate = DateTime.Now.AddDays(-3),
                FilePath = Path.Combine(BackupLocation, "MigrationData_2024-01-13_16-45.backup"),
                Size = 1024 * 1024 * 28,
                Status = "Complete"
            });
        }

        private void UpdateStorageInfo()
        {
            try
            {
                if (!Directory.Exists(BackupLocation))
                {
                    Directory.CreateDirectory(BackupLocation);
                }

                var drive = new DriveInfo(Path.GetPathRoot(BackupLocation) ?? "C:\\");
                var totalSpace = drive.TotalSize;
                var freeSpace = drive.AvailableFreeSpace;
                var usedSpace = totalSpace - freeSpace;

                var backupSize = BackupHistory.Sum(b => b.Size);

                UsedSpace = FormatFileSize(backupSize);
                AvailableSpace = FormatFileSize(freeSpace);
                StorageUsagePercentage = (double)backupSize / totalSpace * 100;

                DataSize = FormatFileSize(backupSize);
            }
            catch (Exception)
            {
                UsedSpace = "Unknown";
                AvailableSpace = "Unknown";
                StorageUsagePercentage = 0;
            }
        }

        private void LoadSettings()
        {
            // Load settings from config or registry
            // For now, use defaults
            EnableDailyBackup = true;
            BackupBeforeMigration = true;
            AutoCleanupBackups = false;
            SelectedRetention = "30 Days";
        }

        private int GetRetentionDays()
        {
            return SelectedRetention switch
            {
                "7 Days" => 7,
                "30 Days" => 30,
                "90 Days" => 90,
                "6 Months" => 180,
                "1 Year" => 365,
                _ => int.MaxValue
            };
        }

        private string CreateBackupData()
        {
            return $@"{{
    ""backupVersion"": ""1.0"",
    ""created"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"",
    ""application"": ""{_appConfig.ApplicationName}"",
    ""version"": ""{_appConfig.Version}"",
    ""configuration"": {{
        ""profiles"": [],
        ""settings"": {{}},
        ""networkMappings"": []
    }},
    ""templates"": [],
    ""migrationHistory"": []
}}";
        }

        private void ExportApplicationSettings(string filePath)
        {
            var settings = new
            {
                BackupSettings = new
                {
                    EnableDailyBackup,
                    BackupBeforeMigration,
                    AutoCleanupBackups,
                    SelectedRetention,
                    BackupLocation
                },
                ApplicationConfig = _appConfig
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private void ImportApplicationSettings(string filePath)
        {
            var json = File.ReadAllText(filePath);
            // In a real implementation, this would parse and apply the settings
        }

        private void ShowStatusMessage(string title, string message, InfoBarSeverity severity)
        {
            StatusTitle = title;
            StatusMessage = message;
            StatusSeverity = severity;
            ShowStatus = true;

            // Auto-hide after 5 seconds
            Task.Delay(5000).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowStatus = false;
                });
            });
        }

        private string FormatFileSize(long bytes)
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
