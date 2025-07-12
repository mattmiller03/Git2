using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;

namespace UiDesktopApp2.Services
{
    public class Logger : ILogger
    {
        public event Action<string, string> MessageWritten;

        public void Info(string message) => Log(message, "INFO");
        public void Warn(string message) => Log(message, "WARN");
        public void Error(string message) => Log(message, "ERROR");

        private void Log(string message, string level)
        {
            var formatted = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
            Debug.WriteLine(formatted);
            MessageWritten?.Invoke(message, level);
        }
    }
}
