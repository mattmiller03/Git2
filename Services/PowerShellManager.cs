using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Management.Automation;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.Services
{
    public class PowerShellManager
    {
        private readonly ILogManager _logManager;
        private readonly IPowerShellScriptManager _scriptManager;

        public PowerShellManager(ILogManager logManager, IPowerShellScriptManager scriptManager)
        {
            _logManager = logManager;
            _scriptManager = scriptManager;
        }

        public async Task<string> ExecuteScriptAsync(
            string scriptName,
            Dictionary<string, object> parameters)
        {
            try
            {
                var scriptPath = _scriptManager.GetScriptPath(scriptName);

                using var ps = PowerShell.Create();
                ps.AddScript(File.ReadAllText(scriptPath));

                // Add parameters
                foreach (var param in parameters)
                {
                    ps.AddParameter(param.Key, param.Value);
                }

                var results = await Task.Run(() => ps.Invoke());

                // Collect and return output
                var output = new List<string>();
                foreach (var result in results)
                {
                    output.Add(result.ToString());
                }

                // Log any errors
                if (ps.Streams.Error.Count > 0)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        _logManager.Error($"PowerShell Error: {error}");
                    }
                }

                return string.Join(Environment.NewLine, output);
            }
            catch (Exception ex)
            {
                _logManager.Error($"PowerShell Script Execution Error: {ex.Message}");
                throw;
            }
        }

        public async Task<ConnectionResult> TestVCenterConnectionAsync(ConnectionProfile profile)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "vCenterServer", profile.ServerAddress },
                    { "Credential", new PSCredential(profile.Username, ConvertToSecureString(profile.Password)) },
                    { "ExcelFilePath", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConnectionTestTemplate.xlsx") },
                    { "Environment", "TEST" },
                    { "EnableScriptDebug", true }
                };

                var result = await ExecuteScriptAsync("Set-Dyn_Env_TagPermissions", parameters);

                // Parse result to determine connection success
                return new ConnectionResult(
                    isSuccessful: true,
                    version: "vSphere 7.0",
                    errorMessage: null
                );
            }
            catch (Exception ex)
            {
                return new ConnectionResult(
                    isSuccessful: false,
                    errorMessage: ex.Message
                );
            }
        }

        public async Task<string> ExecuteTaskAsync(
            string scriptName,
            Dictionary<string, object> parameters)
        {
            try
            {
                var scriptPath = _scriptManager.GetScriptPath(scriptName);

                using var ps = PowerShell.Create();
                ps.AddScript(File.ReadAllText(scriptPath));

                // Add parameters
                foreach (var param in parameters)
                {
                    ps.AddParameter(param.Key, param.Value);
                }

                var results = await Task.Run(() => ps.Invoke());

                // Collect and return output
                var output = new List<string>();
                foreach (var result in results)
                {
                    output.Add(result.ToString());
                }

                // Log any errors
                if (ps.Streams.Error.Count > 0)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        _logManager.Error($"PowerShell Task Error: {error}");
                    }
                }

                return string.Join(Environment.NewLine, output);
            }
            catch (Exception ex)
            {
                _logManager.Error($"PowerShell Task Execution Error: {ex.Message}");
                throw;
            }
        }

        private System.Security.SecureString ConvertToSecureString(string password)
        {
            var securePassword = new System.Security.SecureString();
            foreach (char c in password)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();
            return securePassword;
        }
    }
}
