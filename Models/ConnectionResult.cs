using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp2.Models
{
    internal class ConnectionResult
    {
        public bool IsConnected { get; set; }
        public string Version { get; set; }
        public string ErrorMessage { get; set; }

    }
}
