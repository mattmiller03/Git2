using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;

namespace UiDesktopApp2.Services
{
    public class JsonProfileManager : IProfileManager
    {
        private readonly string _filePath;
        public List<ConnectionProfile> _profiles;

        public JsonProfileManager()
        {
            _filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VCenterMigrationTool",
                "profiles.json"
            );

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            // Load or initialize profiles
            _profiles = LoadProfiles();
        }

        public List<ConnectionProfile> LoadProfiles()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<List<ConnectionProfile>>(json)
                           ?? new List<ConnectionProfile>();
                }
                return new List<ConnectionProfile>();
            }
            catch (Exception ex)
            {
                // Log the error or handle it appropriately
                System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex.Message}");
                return new List<ConnectionProfile>();
            }
        }

        public IEnumerable<ConnectionProfile> GetAllProfiles() =>
            _profiles.ToList();

        public ConnectionProfile? GetProfile(string name) =>
            _profiles.FirstOrDefault(p => p.Name == name);

        public void SaveProfile(ConnectionProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            // Remove existing profile with the same name
            _profiles.RemoveAll(p => p.Name == profile.Name);

            // Add new or updated profile
            _profiles.Add(profile);
            Persist();
        }

        public void DeleteProfile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be null or empty", nameof(name));

            int removedCount = _profiles.RemoveAll(p => p.Name == name);

            if (removedCount > 0)
            {
                Persist();
            }
        }

        private void Persist()
        {
            try
            {
                var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Ensure directory exists before writing
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                // Log the error or handle it appropriately
                System.Diagnostics.Debug.WriteLine($"Error saving profiles: {ex.Message}");
            }
        }

        // Optional: Method to check if a profile exists
        public bool ProfileExists(string name) =>
            _profiles.Any(p => p.Name == name);
    }
}
