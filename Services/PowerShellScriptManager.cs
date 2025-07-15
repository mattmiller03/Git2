using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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

        public void RegisterScript(string scriptName, string relativeScriptPath)
        {
            try
            {
                string fullPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeScriptPath));

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

        /// <summary>
        /// Registers all PowerShell scripts (*.ps1) found recursively in the given scripts root folder.
        /// Uses the filename without extension as script key.
        /// </summary>
        /// <param name="scriptsRootRelativePath">Relative path to the scripts root folder, e.g. "Scripts"</param>
        public void RegisterAllScripts(string scriptsRootRelativePath = "Scripts")
        {
            try
            {
                string scriptsRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptsRootRelativePath));
                if (!Directory.Exists(scriptsRoot))
                {
                    _logger.LogError($"Scripts root folder not found: {scriptsRoot}");
                    throw new DirectoryNotFoundException($"Scripts root folder not found: {scriptsRoot}");
                }

                foreach (var fullPath in Directory.GetFiles(scriptsRoot, "*.ps1", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, fullPath);
                    string scriptName = Path.GetFileNameWithoutExtension(fullPath);

                    // To avoid duplicate keys, optionally you could include subfolder names or enforce unique names
                    if (_scripts.ContainsKey(scriptName))
                    {
                        _logger.LogWarning($"Duplicate script name detected: {scriptName}. Skipping registration of {relativePath}");
                        continue;
                    }

                    RegisterScript(scriptName, relativePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register all PowerShell scripts");
                throw;
            }
        }
    }
}
