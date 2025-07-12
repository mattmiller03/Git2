using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;

namespace UiDesktopApp2.Services
{
    public class JsonProfileManager : IProfileManager
    {
        private readonly string _filePath;
        private List<ConnectionProfile> _profiles;

        public JsonProfileManager()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.json");
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _profiles = JsonSerializer.Deserialize<List<ConnectionProfile>>(json)
                            ?? new List<ConnectionProfile>();
            }
            else
            {
                _profiles = new List<ConnectionProfile>();
            }
        }

        public IEnumerable<ConnectionProfile> GetAllProfiles() =>
            _profiles.ToList();

        public ConnectionProfile GetProfile(string name) =>
            _profiles.FirstOrDefault(p => p.Name == name);

        public void SaveProfile(ConnectionProfile profile)
        {
            var existing = _profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existing != null)
                _profiles.Remove(existing);

            _profiles.Add(profile);
            Persist();
        }

        public void DeleteProfile(string name)
        {
            var existing = _profiles.FirstOrDefault(p => p.Name == name);
            if (existing != null)
            {
                _profiles.Remove(existing);
                Persist();
            }
        }

        private void Persist()
        {
            var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath!, json);
        }
    }
}
