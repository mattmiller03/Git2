using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.Helpers
{
    public interface IBackupManager
    {
        // Legacy methods
        void PerformBackup(string sourcePath, string destinationPath);
        bool ValidateBackup(string backupPath);

        // New VM-specific methods
        Task<BackupResult> PerformVMBackupAsync(string? customBackupPath = null, string? customLogPath = null);
        Task<List<VirtualMachineInfo>> GetVMInventoryAsync();
        bool DeleteBackup(BackupItem backupItem);
        Task<bool> RestoreBackupAsync(BackupItem backupItem, string restoreLocation);

        // Properties
        System.Collections.ObjectModel.ObservableCollection<BackupItem> BackupItems { get; }
        string DefaultBackupPath { get; set; }
        string DefaultLogPath { get; set; }
    }
}
