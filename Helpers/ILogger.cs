using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp2.Helpers
{
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);

        /// <summary>
        /// Raised whenever a message is written.
        /// (msg, level) e.g. ("Connected","INFO")
        /// </summary>
        event Action<string, string>? MessageWritten;
    }
}
