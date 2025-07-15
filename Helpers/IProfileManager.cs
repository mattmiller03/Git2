using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Models;

namespace UiDesktopApp2.Helpers
{
    public interface IProfileManager
    {
        IEnumerable<ConnectionProfile> GetAllProfiles();
        ConnectionProfile? GetProfile(string name);
        void SaveProfile(ConnectionProfile profile);
        void DeleteProfile(string name);
    }
}
