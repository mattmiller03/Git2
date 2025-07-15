using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UiDesktopApp2.Services;


namespace UiDesktopApp2.Models
{
    public class ChangeLogEntry
    {
        public string Version { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty; // "Feature", "Bug Fix", "Release", etc.
        public string Description { get; set; } = string.Empty;
    }
}
