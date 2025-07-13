using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Services;

// Helpers/IPowerShellScriptManager.cs
public interface IPowerShellScriptManager
{
    /// <summary>
    /// Register a script by name (e.g. "HOST-Migrate") so GetScriptPath can resolve it later.
    /// </summary>
    void RegisterScript(string scriptName, string scriptPath);

    /// <summary>
    /// Returns the fully‐qualified disk path for a previously registered script name.
    /// </summary>
    string GetScriptPath(string scriptName);
}
