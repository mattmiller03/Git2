using Meziantou.Framework.Win32;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Text.Json;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.Services
{
    public class BackupManager : IBackupManager
    {
        private readonly ILogger<BackupManager> _logger;
        private readonly PowerShellManager _powerShellManager;
        private readonly ConnectionManager _connectionManager;
        private readonly ICredentialManager _credentialManager;

        public ObservableCollection<BackupItem> BackupItems { get; } = new();
        public string DefaultBackupPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "vCenter Migration", "Backups");
        public string DefaultLogPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "vCenter Migration", "Logs");

        public BackupManager(
            ILogger<BackupManager> logger,
            PowerShellManager powerShellManager,
            ConnectionManager connectionManager)
        {
            _logger = logger;
            _powerShellManager = powerShellManager;
            _connectionManager = connectionManager;

            EnsureDirectoriesExist();
            LoadExistingBackups();
        }

        /// <summary>
        /// Perform VM metadata backup for current vCenter connection
        /// </summary>
        public async Task<BackupResult> PerformVMBackupAsync(string? customBackupPath = null, string? customLogPath = null)
        {
            try
            {
                if (_connectionManager.CurrentConnection == null)
                {
                    return new BackupResult(false, "No active vCenter connection");
                }

                var backupPath = customBackupPath ?? DefaultBackupPath;
                var logPath = customLogPath ?? DefaultLogPath;

                _logger.LogInformation("Starting VM backup for {Server}", _connectionManager.CurrentConnection.ServerAddress);

                // Get credentials for current connection
                var securePassword = _credentialManager.GetPassword(_connectionManager.CurrentConnection.Name);

                if (securePassword.Length == 0)
                {
                    return new BackupResult(false, "No credentials available for current connection");
                }

                var credential = new PSCredential(
                    _connectionManager.CurrentConnection.Username,
                    securePassword);

                // Execute backup
                var result = await _powerShellManager.ExecuteVMBackupAsync(
                    _connectionManager.CurrentConnection,
                    credential,
                    logPath,
                    backupPath);

                if (result.IsSuccessful && !string.IsNullOrEmpty(result.OutputFilePath))
                {
                    // Create backup item
                    var backupItem = CreateBackupItem(result.OutputFilePath, _connectionManager.CurrentConnection.ServerAddress);
                    BackupItems.Add(backupItem);

                    _logger.LogInformation("VM backup completed successfully: {FilePath}", result.OutputFilePath);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VM backup failed");
                return new BackupResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Perform backup with custom source and destination (legacy interface)
        /// </summary>
        public void PerformBackup(string sourcePath, string destinationPath)
        {
            try
            {
                _logger.LogInformation("Performing file backup from {Source} to {Destination}", sourcePath, destinationPath);

                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"Source file not found: {sourcePath}");
                }

                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                File.Copy(sourcePath, destinationPath, true);

                var backupItem = CreateBackupItem(destinationPath, "File Copy");
                BackupItems.Add(backupItem);

                _logger.LogInformation("File backup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File backup failed");
                throw;
            }
        }

        /// <summary>
        /// Validate backup file integrity
        /// </summary>
        public bool ValidateBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger.LogError("Backup file not found: {Path}", backupPath);
                    return false;
                }

                var fileInfo = new FileInfo(backupPath);
                if (fileInfo.Length == 0)
                {
                    _logger.LogError("Backup file is empty: {Path}", backupPath);
                    return false;
                }

                // For JSON files, try to parse to ensure validity
                if (Path.GetExtension(backupPath).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var content = File.ReadAllText(backupPath);
                    JsonSerializer.Deserialize<object>(content);
                }

                _logger.LogInformation("Backup validation successful: {Path}", backupPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup validation failed for {Path}", backupPath);
                return false;
            }
        }

        /// <summary>
        /// Get VM inventory for current connection
        /// </summary>
        public async Task<List<VirtualMachineInfo>> GetVMInventoryAsync()
        {
            try
            {
                if (_connectionManager.CurrentConnection == null)
                {
                    _logger.LogWarning("No active vCenter connection for inventory retrieval");
                    return new List<VirtualMachineInfo>();
                }

                var credentialManager = ServiceLocator.GetService<ICredentialManager>();
                var securePassword = credentialManager.GetPassword(_connectionManager.CurrentConnection.Name);

                if (securePassword.Length == 0)
                {
                    _logger.LogWarning("No credentials available for current connection");
                    return new List<VirtualMachineInfo>();
                }

                var credential = new System.Management.Automation.PSCredential(
                    _connectionManager.CurrentConnection.Username,
                    securePassword);

                return await _powerShellManager.GetVMInventoryAsync(_connectionManager.CurrentConnection, credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve VM inventory");
                return new List<VirtualMachineInfo>();
            }
        }

        /// <summary>
        /// Delete backup item
        /// </summary>
        public bool DeleteBackup(BackupItem backupItem)
        {
            try
            {
                if (File.Exists(backupItem.FilePath))
                {
                    File.Delete(backupItem.FilePath);
                }

                BackupItems.Remove(backupItem);
                _logger.LogInformation("Deleted backup: {Name}", backupItem.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete backup: {Name}", backupItem.Name);
                return false;
            }
        }

        /// <summary>
        /// Restore backup to specified location
        /// </summary>
        public async Task<bool> RestoreBackupAsync(BackupItem backupItem, string restoreLocation)
        {
            try
            {
                if (!File.Exists(backupItem.FilePath))
                {
                    _logger.LogError("Backup file not found: {Path}", backupItem.FilePath);
                    return false;
                }

                var destinationPath = Path.Combine(restoreLocation, Path.GetFileName(backupItem.FilePath));

                await Task.Run(() => File.Copy(backupItem.FilePath, destinationPath, true));

                _logger.LogInformation("Backup restored to: {Path}", destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore backup: {Name}", backupItem.Name);
                return false;
            }
        }

        /// <summary>
        /// Create backup item from file path
        /// </summary>
        private BackupItem CreateBackupItem(string filePath, string source)
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            return new BackupItem
            {
                Name = fileName,
                Type = DetermineBackupType(filePath),
                CreatedDate = fileInfo.CreationTime,
                FilePath = filePath,
                Size = fileInfo.Length,
                Status = "Completed"
            };
        }

        /// <summary>
        /// Determine backup type from file path
        /// </summary>
        private string DetermineBackupType(string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            if (fileName.Contains("VM_Backup_Report"))
                return "VM Metadata";
            else if (fileName.Contains("Network_Report"))
                return "Network Config";
            else if (fileName.Contains("Host_Report"))
                return "Host Config";
            else
                return "General";
        }

        /// <summary>
        /// Ensure required directories exist
        /// </summary>
        private void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(DefaultBackupPath);
                Directory.CreateDirectory(DefaultLogPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup directories");
            }
        }

        /// <summary>
        /// Load existing backups from backup directory
        /// </summary>
        private void LoadExistingBackups()
        {
            try
            {
                if (!Directory.Exists(DefaultBackupPath))
                    return;

                var backupFiles = Directory.GetFiles(DefaultBackupPath, "*.json");

                foreach (var file in backupFiles)
                {
                    try
                    {
                        var backupItem = CreateBackupItem(file, "Existing");
                        BackupItems.Add(backupItem);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load backup item: {File}", file);
                    }
                }

                _logger.LogInformation("Loaded {Count} existing backup items", BackupItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load existing backups");
            }
        }
    }
}
