using System;
using System.Collections.Generic;
using System.IO;
using UiDesktopApp2.Helpers;
using Microsoft.Extensions.Logging;

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
                // Ensure the script path is a full path
                string fullPath = Path.IsPathRooted(scriptPath)
                    ? scriptPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);

                // Validate script file exists
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning($"PowerShell script not found: {fullPath}");
                    throw new FileNotFoundException($"PowerShell script not found: {fullPath}", fullPath);
                }

                _scripts[scriptName] = fullPath;
                _logger.LogInformation($"Registered PowerShell script: {scriptName} at {fullPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering script {scriptName}");
                throw;
            }
        }

        public string GetScriptPath(string scriptName)
        {
            try
            {
                if (_scripts.TryGetValue(scriptName, out var scriptPath))
                {
                    return scriptPath;
                }

                _logger.LogWarning($"No script registered with name: {scriptName}");
                throw new KeyNotFoundException($"No script registered with name: {scriptName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving script path for {scriptName}");
                throw;
            }
        }

        /// <summary>
        /// Lists all registered scripts
        /// </summary>
        /// <returns>Dictionary of registered script names and their paths</returns>
        public IReadOnlyDictionary<string, string> GetRegisteredScripts()
        {
            return new Dictionary<string, string>(_scripts);
        }

        /// <summary>
        /// Bulk script registration method
        /// </summary>
        /// <param name="scripts">Dictionary of script names and paths</param>
        public void RegisterScripts(Dictionary<string, string> scripts)
        {
            foreach (var script in scripts)
            {
                RegisterScript(script.Key, script.Value);
            }
        }
    }
}
