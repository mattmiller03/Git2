using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation; // Ensure you have the System.Management.Automation NuGet package installed
using UiDesktopApp2.Helpers;
using System.Management.Automation.Runspaces; // For PowerShell runspaces


namespace UiDesktopApp2.Models
{
    public class PowerShellManager
    {
        private readonly ILogger _logger;
        private readonly IPowerShellScriptManager _scriptMgr;

        public PowerShellManager(ILogger logger, IPowerShellScriptManager scriptMgr)
        {
            _logger = logger;
            _scriptMgr = scriptMgr;
        }

        public async Task<string> ExecuteAsync(string scriptName, Dictionary<string, object> parameters)
        {
            var scriptPath = _scriptMgr.GetScriptPath(scriptName);
            using var ps = PowerShell.Create(); // Ensure System.Management.Automation is referenced
            ps.AddCommand(scriptPath);

            foreach (var kvp in parameters)
                ps.AddParameter(kvp.Key, kvp.Value);

            var results = await Task.Run(() => ps.Invoke());
            if (ps.Streams.Error.Count > 0)
                foreach (var err in ps.Streams.Error)
                    _logger.Error(err.ToString());

            var sb = new StringBuilder();
            foreach (var r in results)
                sb.AppendLine(r.ToString());

            return sb.ToString();
        }
    }
}
