using Microsoft.Extensions.Logging;
using System;
using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, string> _scripts = new();
        private readonly ILogger<PowerShellScriptManager> _logger;

        public PowerShellScriptManager(ILogger<PowerShellScriptManager> logger)
        {
            _logger = logger;
        }

        public void RegisterScript(string scriptName, string scriptPath)
        {
            try
            {
                string fullPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath));

                if (!File.Exists(fullPath))
                {
                    _logger.LogError($"Script file not found: {fullPath}");
                    throw new FileNotFoundException($"PowerShell script not found: {fullPath}", fullPath);
                }

                _scripts[scriptName] = fullPath;
                _logger.LogInformation($"Registered PowerShell script: {scriptName} at {fullPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to register script {scriptName}");
                throw;
            }
        }

        public string GetScriptPath(string scriptName)
        {
            try
            {
                if (_scripts.TryGetValue(scriptName, out var path))
                {
                    return path;
                }
                throw new KeyNotFoundException($"Script '{scriptName}' is not registered");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get script path for {scriptName}");
                throw;
            }
        }
    }
}
