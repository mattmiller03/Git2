using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using System.Linq;
using System.Text.Json;

namespace UiDesktopApp2.Services
{
    public class PowerShellManager
    {
        private readonly ILogger<PowerShellManager> _logger;
        private readonly IPowerShellScriptManager _scriptManager;
        private readonly Dictionary<string, object> _activeConnections = new();

        public PowerShellManager(
            ILogger<PowerShellManager> logger,
            IPowerShellScriptManager scriptManager)
        {
            _logger = logger;
            _scriptManager = scriptManager;
        }

        /// <summary>
        /// Test vCenter connection using PowerShell script
        /// </summary>
        public async Task<ConnectionResult> TestVCenterConnectionAsync(ConnectionProfile profile, PSCredential credential)
        {
            try
            {
                _logger.LogInformation("Testing vCenter connection to {Server}", profile.ServerAddress);

                var parameters = new Dictionary<string, object>
                {
                    { "vCenterServer", profile.ServerAddress },
                    { "Credential", credential }
                };

                var result = await ExecuteScriptAsync("Test-VcConnection", parameters);
                var connectionResult = ParseConnectionResult(result);

                if (connectionResult.IsSuccessful)
                {
                    _logger.LogInformation("vCenter connection test successful for {Server}", profile.ServerAddress);
                }
                else
                {
                    _logger.LogError("vCenter connection test failed for {Server}: {Error}",
                        profile.ServerAddress, connectionResult.ErrorMessage);
                }

                return connectionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "vCenter connection test failed for {Server}", profile.ServerAddress);
                return new ConnectionResult(false, errorMessage: ex.Message);
            }
        }

        /// <summary>
        /// Disconnect from vCenter server
        /// </summary>
        public async Task<bool> DisconnectVCenterAsync(ConnectionProfile profile)
        {
            try
            {
                _logger.LogInformation("Disconnecting from vCenter {Server}", profile.ServerAddress);

                var parameters = new Dictionary<string, object>
                {
                    { "vCenterServer", profile.ServerAddress }
                };

                // Create a simple disconnect script call
                using var ps = PowerShell.Create();
                ps.AddScript($"Disconnect-VIServer -Server '{profile.ServerAddress}' -Confirm:$false -Force -ErrorAction SilentlyContinue");

                await Task.Run(() => ps.Invoke());

                _activeConnections.Remove(profile.ServerAddress);
                _logger.LogInformation("Successfully disconnected from vCenter {Server}", profile.ServerAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect from vCenter {Server}", profile.ServerAddress);
                return false;
            }
        }

        /// <summary>
        /// Execute VM backup script and return backup information
        /// </summary>
        public async Task<BackupResult> ExecuteVMBackupAsync(ConnectionProfile profile, PSCredential credential, string logPath, string reportPath)
        {
            try
            {
                _logger.LogInformation("Starting VM backup for vCenter {Server}", profile.ServerAddress);

                // First establish connection
                var connectionResult = await TestVCenterConnectionAsync(profile, credential);
                if (!connectionResult.IsSuccessful)
                {
                    return new BackupResult(false, "Failed to connect to vCenter: " + connectionResult.ErrorMessage);
                }

                // Execute backup script with connection
                var parameters = new Dictionary<string, object>
                {
                    { "vCenterServer", profile.ServerAddress },
                    { "Credential", credential },
                    { "LogOutputLocation", logPath },
                    { "ReportOutputLocation", reportPath }
                };

                var result = await ExecuteVMBackupScriptAsync(parameters);

                _logger.LogInformation("VM backup completed for vCenter {Server}", profile.ServerAddress);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VM backup failed for vCenter {Server}", profile.ServerAddress);
                return new BackupResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Get VM inventory from vCenter
        /// </summary>
        public async Task<List<VirtualMachineInfo>> GetVMInventoryAsync(ConnectionProfile profile, PSCredential credential)
        {
            try
            {
                _logger.LogInformation("Retrieving VM inventory from vCenter {Server}", profile.ServerAddress);

                var parameters = new Dictionary<string, object>
                {
                    { "vCenterServer", profile.ServerAddress },
                    { "Credential", credential }
                };

                var script = @"
                    param($vCenterServer, $Credential)
                    
                    try {
                        if (-not (Get-Module -Name VMware.VimAutomation.Core)) {
                            Import-Module VMware.VimAutomation.Core -ErrorAction Stop
                        }

                        $connection = Connect-VIServer -Server $vCenterServer -Credential $Credential -ErrorAction Stop
                        
                        $vms = Get-VM -Server $connection | Select-Object Name, PowerState, 
                            @{N='GuestOS';E={$_.Guest.OSFullName}},
                            @{N='CpuCount';E={$_.NumCpu}},
                            @{N='MemoryGB';E={$_.MemoryGB}},
                            @{N='StorageGB';E={[math]::Round($_.ProvisionedSpaceGB, 2)}},
                            @{N='Cluster';E={$_.VMHost.Parent.Name}},
                            @{N='Host';E={$_.VMHost.Name}},
                            @{N='ToolsStatus';E={$_.Guest.ToolsStatus}}
                        
                        $vms | ConvertTo-Json -Depth 3
                        
                        Disconnect-VIServer -Server $vCenterServer -Confirm:$false -Force
                    }
                    catch {
                        Write-Error ""Failed to retrieve VM inventory: $($_.Exception.Message)""
                    }
                ";

                var result = await ExecuteScriptDirectAsync(script, parameters);
                return ParseVMInventory(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve VM inventory from {Server}", profile.ServerAddress);
                return new List<VirtualMachineInfo>();
            }
        }

        /// <summary>
        /// Execute PowerShell script with parameters
        /// </summary>
        private async Task<string> ExecuteScriptAsync(string scriptName, Dictionary<string, object> parameters)
        {
            var scriptPath = _scriptManager.GetScriptPath(scriptName);

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

        /// <summary>
        /// Execute PowerShell script directly from string
        /// </summary>
        private async Task<string> ExecuteScriptDirectAsync(string script, Dictionary<string, object> parameters)
        {
            using var ps = PowerShell.Create();
            ps.AddScript(script);

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

        /// <summary>
        /// Execute VM backup script with special handling for vCenter connection
        /// </summary>
        private async Task<BackupResult> ExecuteVMBackupScriptAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var vCenterServer = parameters["vCenterServer"].ToString();
                var credential = (PSCredential)parameters["Credential"];
                var logPath = parameters["LogOutputLocation"].ToString();
                var reportPath = parameters["ReportOutputLocation"].ToString();

                var script = @"
                    param($vCenterServer, $Credential, $LogOutputLocation, $ReportOutputLocation)
                    
                    try {
                        if (-not (Get-Module -Name VMware.VimAutomation.Core)) {
                            Import-Module VMware.VimAutomation.Core -ErrorAction Stop
                        }

                        $connection = Connect-VIServer -Server $vCenterServer -Credential $Credential -ErrorAction Stop
                        
                        # Execute the VM backup script content here
                        $scriptPath = Get-ChildItem -Path . -Filter 'VMBackup.ps1' -Recurse | Select-Object -First 1
                        if ($scriptPath) {
                            & $scriptPath.FullName -vCenterConnection $connection -LogOutputLocation $LogOutputLocation -ReportOutputLocation $ReportOutputLocation
                        } else {
                            throw 'VMBackup.ps1 script not found'
                        }
                    }
                    catch {
                        Write-Error ""VM backup failed: $($_.Exception.Message)""
                        throw
                    }
                ";

                var result = await ExecuteScriptDirectAsync(script, parameters);

                // Parse the result to extract file paths
                var outputFile = ExtractOutputFilePath(result);
                var logFile = ExtractLogFilePath(result);

                return new BackupResult(true, "VM backup completed successfully", outputFile, logFile);
            }
            catch (Exception ex)
            {
                return new BackupResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Parse connection result from PowerShell output
        /// </summary>
        private ConnectionResult ParseConnectionResult(string scriptOutput)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(scriptOutput))
                {
                    return new ConnectionResult(false, errorMessage: "No output from connection test");
                }

                if (scriptOutput.Contains("Connected successfully"))
                {
                    var version = "unknown";
                    var versionStart = scriptOutput.IndexOf("Version:") + "Version:".Length;
                    if (versionStart > "Version:".Length - 1)
                    {
                        var versionEnd = scriptOutput.IndexOf('\n', versionStart);
                        if (versionEnd > versionStart)
                        {
                            version = scriptOutput.Substring(versionStart, versionEnd - versionStart).Trim();
                        }
                    }

                    return new ConnectionResult(true, version);
                }

                return new ConnectionResult(false, errorMessage: scriptOutput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse connection result");
                return new ConnectionResult(false, errorMessage: "Failed to parse connection result: " + ex.Message);
            }
        }

        /// <summary>
        /// Parse VM inventory from JSON output
        /// </summary>
        private List<VirtualMachineInfo> ParseVMInventory(string jsonOutput)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonOutput))
                    return new List<VirtualMachineInfo>();

                var vmData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonOutput);
                if (vmData == null)
                    return new List<VirtualMachineInfo>();

                return vmData.Select(vm => new VirtualMachineInfo
                {
                    Name = vm.GetValueOrDefault("Name", "").ToString() ?? "",
                    PowerState = vm.GetValueOrDefault("PowerState", "").ToString() ?? "",
                    GuestOS = vm.GetValueOrDefault("GuestOS", "").ToString() ?? "",
                    CpuCount = Convert.ToInt32(vm.GetValueOrDefault("CpuCount", 0)),
                    MemoryGB = Convert.ToInt32(vm.GetValueOrDefault("MemoryGB", 0)),
                    StorageGB = Convert.ToInt32(vm.GetValueOrDefault("StorageGB", 0)),
                    Cluster = vm.GetValueOrDefault("Cluster", "").ToString() ?? "",
                    Host = vm.GetValueOrDefault("Host", "").ToString() ?? "",
                    ToolsStatus = vm.GetValueOrDefault("ToolsStatus", "").ToString() ?? ""
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse VM inventory");
                return new List<VirtualMachineInfo>();
            }
        }

        /// <summary>
        /// Extract output file path from script result
        /// </summary>
        private string? ExtractOutputFilePath(string scriptOutput)
        {
            try
            {
                var marker = "JSON file saved to: ";
                var startIndex = scriptOutput.IndexOf(marker);
                if (startIndex >= 0)
                {
                    startIndex += marker.Length;
                    var endIndex = scriptOutput.IndexOf('\n', startIndex);
                    if (endIndex > startIndex)
                    {
                        return scriptOutput.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extract log file path from script result
        /// </summary>
        private string? ExtractLogFilePath(string scriptOutput)
        {
            // Implementation depends on your log file naming convention
            // This is a placeholder implementation
            return null;
        }
    }

    /// <summary>
    /// Result of VM backup operation
    /// </summary>
    public class BackupResult
    {
        public bool IsSuccessful { get; }
        public string Message { get; }
        public string? OutputFilePath { get; }
        public string? LogFilePath { get; }

        public BackupResult(bool isSuccessful, string message, string? outputFilePath = null, string? logFilePath = null)
        {
            IsSuccessful = isSuccessful;
            Message = message;
            OutputFilePath = outputFilePath;
            LogFilePath = logFilePath;
        }
    }
}
