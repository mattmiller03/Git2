using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;

namespace UiDesktopApp2.Services
{
    public class PowerShellManager
    {
        private readonly ILogger<PowerShellManager> _logger;
        private readonly IPowerShellScriptManager _scriptManager;

        public PowerShellManager(
            ILogger<PowerShellManager> logger,
            IPowerShellScriptManager scriptManager)
        {
            _logger = logger;
            _scriptManager = scriptManager;
        }

        public async Task<ConnectionResult> TestVCenterConnectionAsync(ConnectionProfile profile, PSCredential credential)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "vCenterServer", profile.ServerAddress },
                    { "Credential", credential },
                    { "Environment", "TEST" }
                };

                var result = await ExecuteScriptAsync("Test-VCConnection", parameters);
                return ParseConnectionResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "vCenter connection test failed");
                return new ConnectionResult(false, errorMessage: ex.Message);
            }
        }

        private ConnectionResult ParseConnectionResult(string scriptOutput)
        {
            // Simplified version without ExtractBetween
            if (scriptOutput.Contains("Connected successfully"))
            {
                // Extract version more simply
                var versionStart = scriptOutput.IndexOf("Version:") + "Version:".Length;
                var versionEnd = scriptOutput.IndexOf('\n', versionStart);
                var version = versionEnd > versionStart
                    ? scriptOutput.Substring(versionStart, versionEnd - versionStart).Trim()
                    : "unknown";

                return new ConnectionResult(true, version);
            }
            return new ConnectionResult(false, errorMessage: scriptOutput);
        }

        private async Task<string> ExecuteScriptAsync(string scriptName, Dictionary<string, object> parameters)
        {
            var scriptPath = _scriptManager.GetScriptPath(scriptName); // returns full path including subfolder
            using var ps = PowerShell.Create();

            ps.AddCommand(scriptPath);
            foreach (var param in parameters)
            {
                ps.AddParameter(param.Key, param.Value);
            }

            var results = await Task.Run(() => ps.Invoke());

            if (ps.HadErrors)
            {
                var errors = string.Join(Environment.NewLine, ps.Streams.Error.Select(e => e.ToString()));
                _logger.LogError("PowerShell script errors: {Errors}", errors);
                throw new Exception($"PowerShell script errors: {errors}");
            }

            return string.Join(Environment.NewLine, results.Select(r => r?.ToString() ?? string.Empty));
        }
    }
}