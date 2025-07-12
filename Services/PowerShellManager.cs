using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;

namespace UiDesktopApp2.Services
{
    public class PowerShellScriptManager : IPowerShellScriptManager
    {
        private readonly Dictionary<string, string> _scripts = new Dictionary<string, string>();

        public void RegisterScript(string scriptName, string scriptPath)
        {
            _scripts[scriptName] = scriptPath;
        }

        public string GetScriptPath(string scriptName)
        {
            if (_scripts.TryGetValue(scriptName, out var relativePath))
            {
                // Assume scripts folder is deployed alongside the binary
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            }

            throw new FileNotFoundException($"PowerShell script '{scriptName}' not registered");
        }
    }
}
