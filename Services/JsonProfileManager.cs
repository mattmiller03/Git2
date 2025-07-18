using UiDesktopApp2.Helpers; // Make sure to import the namespace with IProfileManager
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System;

namespace UiDesktopApp2.Services
{
    public class JsonProfileManager : IProfileManager
    {
        private readonly List<ConnectionProfile> _profiles = new();
        private readonly string _profilesFilePath;
        private readonly ILogger<JsonProfileManager> _logger;

        public JsonProfileManager(ILogger<JsonProfileManager> logger)
        {
            _logger = logger;

            // Set up profiles file path
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "vCenterMigrationTool");

            Directory.CreateDirectory(appDataPath);
            _profilesFilePath = Path.Combine(appDataPath, "connection_profiles.json");

            LoadFromFile();
        }

        // Implement all IProfileManager methods

        public IEnumerable<ConnectionProfile> GetAllProfiles()
        {
            return _profiles.ToList(); // Return a copy to prevent external modification
        }

        public ConnectionProfile? GetProfile(string name)
        {
            return _profiles.FirstOrDefault(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public void SaveProfile(ConnectionProfile profile)
        {
            var existingProfile = GetProfile(profile.Name);

            if (existingProfile != null)
            {
                _profiles.Remove(existingProfile);
            }

            _profiles.Add(profile);
            SaveToFile();
        }

        public void DeleteProfile(string name)
        {
            var profile = GetProfile(name);

            if (profile != null)
            {
                _profiles.Remove(profile);
                SaveToFile();
            }
        }

        public IEnumerable<ConnectionProfile> GetProfilesByCategory(string category)
        {
            return _profiles
                .Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public void ExportProfiles(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(_profiles, options);
                File.WriteAllText(filePath, json);
                _logger.LogInformation("Exported {Count} profiles to {FilePath}", _profiles.Count, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting profiles to {FilePath}", filePath);
                throw;
            }
        }

        public void ImportProfiles(string filePath, bool overwrite = false)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Profile import file not found: {filePath}");
                }

                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var importedProfiles = JsonSerializer.Deserialize<List<ConnectionProfile>>(json, options);
                if (importedProfiles == null || !importedProfiles.Any())
                {
                    _logger.LogWarning("No profiles found in import file {FilePath}", filePath);
                    return;
                }

                // Validate imported profiles
                foreach (var profile in importedProfiles)
                {
                    // Replace the Validate call with simple validation
                    if (string.IsNullOrEmpty(profile.Name) ||
                        string.IsNullOrEmpty(profile.ServerAddress) ||
                        string.IsNullOrEmpty(profile.Username))
                    {
                        _logger.LogWarning("Skipping invalid profile {ProfileName}: Missing required fields",
                            profile.Name ?? "[unnamed]");
                        continue;
                    }

                    var existingProfile = _profiles.FirstOrDefault(p =>
                        string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingProfile != null)
                    {
                        if (overwrite)
                        {
                            _profiles.Remove(existingProfile);
                            _profiles.Add(profile);
                            _logger.LogInformation("Overwritten existing profile {ProfileName}", profile.Name);
                        }
                        else
                        {
                            _logger.LogWarning("Skipped importing profile {ProfileName} - already exists", profile.Name);
                        }
                    }
                    else
                    {
                        _profiles.Add(profile);
                        _logger.LogInformation("Imported profile {ProfileName}", profile.Name);
                    }
                }

                SaveToFile();
                _logger.LogInformation("Successfully imported profiles from {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing profiles from {FilePath}", filePath);
                throw;
            }
        }

        // Private helper methods

        private void LoadFromFile()
        {
            try
            {
                if (!File.Exists(_profilesFilePath))
                {
                    _logger.LogInformation("Profiles file not found, creating new file");
                    _profiles.Clear();
                    return;
                }

                var json = File.ReadAllText(_profilesFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogInformation("Profiles file is empty");
                    _profiles.Clear();
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var loadedProfiles = JsonSerializer.Deserialize<List<ConnectionProfile>>(json, options);

                if (loadedProfiles == null)
                {
                    _logger.LogWarning("Failed to deserialize profiles");
                    _profiles.Clear();
                    return;
                }

                _profiles.Clear();
                _profiles.AddRange(loadedProfiles);
                _logger.LogInformation("Loaded {Count} profiles", _profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profiles from file");
                _profiles.Clear();
            }
        }

        private void SaveToFile()
        {
            try
            {
                // Backup existing file before saving
                BackupProfilesFile();

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(_profiles, options);
                File.WriteAllText(_profilesFilePath, json);
                _logger.LogInformation("Saved {Count} profiles to file", _profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profiles to file");
                throw;
            }
        }

        private void BackupProfilesFile()
        {
            try
            {
                if (File.Exists(_profilesFilePath))
                {
                    var backupFolder = Path.Combine(Path.GetDirectoryName(_profilesFilePath) ?? "", "Backups");
                    Directory.CreateDirectory(backupFolder);

                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var backupFile = Path.Combine(backupFolder, $"connection_profiles_{timestamp}.json.bak");

                    File.Copy(_profilesFilePath, backupFile);
                    _logger.LogDebug("Created backup of profiles at {BackupPath}", backupFile);

                    // Clean up old backups (keep last 5)
                    CleanupOldBackups(backupFolder, 5);
                }
            }
            catch (Exception ex)
            {
                // Just log but don't fail the save operation if backup fails
                _logger.LogWarning(ex, "Failed to backup profiles file");
            }
        }

        private void CleanupOldBackups(string backupFolder, int keepCount)
        {
            try
            {
                var backupFiles = Directory.GetFiles(backupFolder, "connection_profiles_*.json.bak")
                    .OrderByDescending(f => f)
                    .Skip(keepCount);

                foreach (var file in backupFiles)
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted old profile backup: {FilePath}", file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup old backup files");
            }
        }
    }
}
