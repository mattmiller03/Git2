using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;

namespace UiDesktopApp2.Services
{
    public class JsonProfileManager : IProfileManager
    {
        private readonly ILogger<JsonProfileManager> _logger;
        private readonly string _profilesFilePath;
        private List<ConnectionProfile> _profiles = new();

        public JsonProfileManager(ILogger<JsonProfileManager> logger)
        {
            _logger = logger;

            // Store profiles in app data folder
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "UiDesktopApp2");
            _profilesFilePath = Path.Combine(appFolder, "connection_profiles.json");

            Directory.CreateDirectory(appFolder);
            LoadProfiles();
        }

        public IEnumerable<ConnectionProfile> GetAllProfiles()
        {
            return _profiles.AsReadOnly();
        }

        public ConnectionProfile? GetProfile(string name)
        {
            return _profiles.FirstOrDefault(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public void SaveProfile(ConnectionProfile profile)
        {
            try
            {
                if (profile == null)
                    throw new ArgumentNullException(nameof(profile));

                // Validate required fields
                if (string.IsNullOrWhiteSpace(profile.Name))
                    throw new ArgumentException("Profile name cannot be empty");

                // Check for duplicate names
                var existingProfile = _profiles
                    .FirstOrDefault(p =>
                        string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase) &&
                        !ReferenceEquals(p, profile));

                if (existingProfile != null)
                    throw new InvalidOperationException($"Profile with name '{profile.Name}' already exists");

                // Add or update profile
                var index = _profiles.FindIndex(p => ReferenceEquals(p, profile));
                if (index >= 0)
                {
                    _profiles[index] = profile;
                }
                else
                {
                    _profiles.Add(profile);
                }

                SaveToFile();
                _logger.LogInformation("Saved profile: {ProfileName}", profile.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile");
                throw;
            }
        }

        public void DeleteProfile(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Profile name cannot be empty", nameof(name));
                }

                _logger.LogInformation($"Attempting to delete profile: '{name}'");

                // Find profile with case-insensitive comparison
                var profile = _profiles.FirstOrDefault(p =>
                    string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                if (profile != null)
                {
                    _profiles.Remove(profile);
                    SaveToFile();
                    _logger.LogInformation("Successfully deleted profile: {ProfileName}", name);
                }
                else
                {
                    _logger.LogWarning("Profile not found for deletion: {ProfileName}", name);
                    // Don't throw an exception, just log the warning
                    // This allows the UI to handle the "not found" case gracefully
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile: {ProfileName}", name);
                throw;
            }
        }

        private void LoadProfiles()
        {
            try
            {
                if (!File.Exists(_profilesFilePath))
                {
                    _profiles = new List<ConnectionProfile>();
                    return;
                }

                var json = File.ReadAllText(_profilesFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                _profiles = JsonSerializer.Deserialize<List<ConnectionProfile>>(json, options) ?? new List<ConnectionProfile>();
                _logger.LogInformation("Loaded {Count} profiles", _profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profiles");
                _profiles = new List<ConnectionProfile>();
            }
        }

        private void SaveToFile()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(_profiles, options);
                File.WriteAllText(_profilesFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profiles to file");
                throw;
            }
        }
    }
}
