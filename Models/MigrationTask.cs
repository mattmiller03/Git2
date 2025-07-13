using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Helpers;

namespace UiDesktopApp2.Models
{
    public class MigrationTask
    {
        public string ObjectName { get; set; } = string.Empty;  // Added default value
        public string ObjectType { get; set; } = string.Empty;  // Added default value
        public MigrationStatus Status { get; set; }
        public double Progress { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Details { get; set; } = string.Empty;  // Added default value
    }
}
