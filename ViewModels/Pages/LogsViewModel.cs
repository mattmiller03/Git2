using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;


namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly AppConfig _appConfig;

        [ObservableProperty]
        private ObservableCollection<string> _logFiles = new();

        [ObservableProperty]
        private string _selectedLogFile = "Application.log";

        [ObservableProperty]
        private ObservableCollection<string> _logLevels = new()
        {
            "All Levels", "Error", "Warning", "Information", "Debug", "Verbose"
        };

        [ObservableProperty]
        private string _selectedLogLevel = "All Levels";

        [ObservableProperty]
        private ObservableCollection<string> _dateFilters = new()
        {
            "All Dates", "Today", "Last 7 Days", "Last 30 Days", "Custom Range"
        };

        [ObservableProperty]
        private string _selectedDateFilter = "All Dates";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _logContent = string.Empty;

        [ObservableProperty]
        private string _fileSize = "0 KB";

        [ObservableProperty]
        private string _lastModified = "Never";

        [ObservableProperty]
        private string _lineCount = "0";

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private bool _isAutoScrollEnabled = true;

        [ObservableProperty]
        private bool _isRealTimeEnabled = false;

        [ObservableProperty]
        private bool _isLoading = false;

        public LogsViewModel(AppConfig appConfig)
        {
            _appConfig = appConfig;
            LoadLogFiles();
            LoadLogContent();
        }

        [RelayCommand]
        private void RefreshLogs()
        {
            LoadLogFiles();
            LoadLogContent();
        }

        [RelayCommand]
        private void OpenLogFolder()
        {
            try
            {
                var logPath = Path.GetDirectoryName(GetCurrentLogFilePath()) ?? _appConfig.LogPath;
                if (Directory.Exists(logPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logPath);
                }
                else
                {
                    StatusText = "Log folder not found";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error opening log folder: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ClearFilter()
        {
            SearchText = string.Empty;
            SelectedLogLevel = "All Levels";
            SelectedDateFilter = "All Dates";
            LoadLogContent();
        }

        [RelayCommand]
        private void ExportSelected()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*",
                    FileName = $"{Path.GetFileNameWithoutExtension(SelectedLogFile)}_export.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, LogContent);
                    StatusText = $"Log exported to {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ExportAll()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*",
                    FileName = $"all_logs_export.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var allContent = string.Empty;
                    foreach (var logFile in LogFiles)
                    {
                        var filePath = Path.Combine(_appConfig.LogPath, logFile);
                        if (File.Exists(filePath))
                        {
                            allContent += $"=== {logFile} ==={Environment.NewLine}";
                            allContent += File.ReadAllText(filePath);
                            allContent += $"{Environment.NewLine}{Environment.NewLine}";
                        }
                    }

                    File.WriteAllText(saveFileDialog.FileName, allContent);
                    StatusText = $"All logs exported to {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ClearLog()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to clear the {SelectedLogFile} file? This action cannot be undone.",
                    "Confirm Clear Log",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var filePath = GetCurrentLogFilePath();
                    if (File.Exists(filePath))
                    {
                        File.WriteAllText(filePath, string.Empty);
                        LoadLogContent();
                        StatusText = $"{SelectedLogFile} cleared successfully";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Clear failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ArchiveLogs()
        {
            try
            {
                var archiveFolder = Path.Combine(_appConfig.LogPath, "Archive", DateTime.Now.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(archiveFolder);

                var archivedCount = 0;
                foreach (var logFile in LogFiles)
                {
                    var sourcePath = Path.Combine(_appConfig.LogPath, logFile);
                    var destPath = Path.Combine(archiveFolder, logFile);

                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, destPath, true);
                        File.WriteAllText(sourcePath, string.Empty); // Clear original
                        archivedCount++;
                    }
                }

                LoadLogContent();
                StatusText = $"{archivedCount} log files archived to {archiveFolder}";
            }
            catch (Exception ex)
            {
                StatusText = $"Archive failed: {ex.Message}";
            }
        }

        partial void OnSelectedLogFileChanged(string value)
        {
            LoadLogContent();
        }

        partial void OnSelectedLogLevelChanged(string value)
        {
            LoadLogContent();
        }

        partial void OnSelectedDateFilterChanged(string value)
        {
            LoadLogContent();
        }

        partial void OnSearchTextChanged(string value)
        {
            LoadLogContent();
        }

        private void LoadLogFiles()
        {
            try
            {
                LogFiles.Clear();

                var logPath = _appConfig.LogPath;
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }

                var logFiles = Directory.GetFiles(logPath, "*.log")
                    .Select(Path.GetFileName)
                    .Where(f => f != null)
                    .Cast<string>()
                    .OrderBy(f => f)
                    .ToList();

                // Add default log files if they don't exist
                var defaultLogFiles = new[] { "Application.log", "Migration.log", "PowerShell.log", "Error.log", "Debug.log" };
                foreach (var defaultFile in defaultLogFiles)
                {
                    if (!logFiles.Contains(defaultFile))
                    {
                        logFiles.Add(defaultFile);
                        // Create empty log file
                        File.WriteAllText(Path.Combine(logPath, defaultFile), string.Empty);
                    }
                }

                foreach (var logFile in logFiles.OrderBy(f => f))
                {
                    LogFiles.Add(logFile);
                }

                StatusText = $"Found {LogFiles.Count} log files";
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading log files: {ex.Message}";
            }
        }

        private void LoadLogContent()
        {
            try
            {
                IsLoading = true;
                StatusText = "Loading log content...";

                var filePath = GetCurrentLogFilePath();
                if (!File.Exists(filePath))
                {
                    LogContent = $"Log file not found: {filePath}";
                    FileSize = "0 KB";
                    LastModified = "Never";
                    LineCount = "0";
                    StatusText = "Log file not found";
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                FileSize = FormatFileSize(fileInfo.Length);
                LastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                var content = File.ReadAllText(filePath);
                var lines = content.Split('\n');
                LineCount = lines.Length.ToString("N0");

                // Apply filters
                var filteredLines = ApplyFilters(lines);
                LogContent = string.Join(Environment.NewLine, filteredLines);

                StatusText = $"Loaded {filteredLines.Count} lines (filtered from {lines.Length} total)";
            }
            catch (Exception ex)
            {
                LogContent = $"Error loading log file: {ex.Message}";
                StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<string> ApplyFilters(string[] lines)
        {
            var filtered = lines.AsEnumerable();

            // Apply log level filter
            if (SelectedLogLevel != "All Levels")
            {
                filtered = filtered.Where(line =>
                    line.Contains($"[{SelectedLogLevel.ToUpper()}]", StringComparison.OrdinalIgnoreCase));
            }

            // Apply date filter
            if (SelectedDateFilter != "All Dates")
            {
                var filterDate = GetFilterDate();
                if (filterDate.HasValue)
                {
                    filtered = filtered.Where(line => LineMatchesDateFilter(line, filterDate.Value));
                }
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(line =>
                    line.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Limit to last 1000 lines for performance
            return filtered.TakeLast(1000).ToList();
        }

        private DateTime? GetFilterDate()
        {
            return SelectedDateFilter switch
            {
                "Today" => DateTime.Today,
                "Last 7 Days" => DateTime.Today.AddDays(-7),
                "Last 30 Days" => DateTime.Today.AddDays(-30),
                _ => null
            };
        }

        private bool LineMatchesDateFilter(string line, DateTime filterDate)
        {
            // Try to extract date from log line format: [2024-01-15 14:30:25.123]
            var match = System.Text.RegularExpressions.Regex.Match(line, @"\[ (\d{4}-\d{2}-\d{2})\s");
            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var lineDate))
            {
                return lineDate >= filterDate;
            }
            return false;
        }

        private string GetCurrentLogFilePath()
        {
            return Path.Combine(_appConfig.LogPath, SelectedLogFile);
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
