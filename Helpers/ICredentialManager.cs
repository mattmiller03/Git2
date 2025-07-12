using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp2.Helpers
{
    public interface ICredentialManager
    {
        /// <summary>
        /// Persists the given password under a key (typically your profile name).
        /// </summary>
        /// <param name="key">Unique key, e.g. “VCenterMigration_<profileName>”.</param>
        /// <param name="userName">Username associated with the credential.</param>
        /// <param name="password">Password to store as SecureString.</param>
        void SavePassword(string key, string userName, SecureString password);

        /// <summary>
        /// Retrieves the stored password for the given key, or an empty SecureString if none exists.
        /// </summary>
        SecureString GetPassword(string key);

        /// <summary>
        /// Deletes the stored credential for the given key.
        /// </summary>
        void DeletePassword(string key);
    }
}
