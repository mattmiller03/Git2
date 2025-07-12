using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp2.Helpers
{
    /// <summary>  
    /// Interface for backup management functionality.  
    /// </summary>  
    public interface IBackupManager
    {
        // Define methods and properties that BackupManager must implement.  
        void PerformBackup(string sourcePath, string destinationPath);
        bool ValidateBackup(string backupPath);
    }
}
