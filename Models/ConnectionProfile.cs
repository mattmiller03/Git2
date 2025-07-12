using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;

namespace UiDesktopApp2.Models
{
    public class ConnectionProfile
    {
        public string Name { get; set; }
        public string SourceVCenter { get; set; }
        public string SourceUsername { get; set; }
        public string DestinationVCenter { get; set; }
        public string DestinationUsername { get; set; }

    }
}
