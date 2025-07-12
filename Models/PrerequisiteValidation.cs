using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp2.Models
{
    public class PrerequisiteValidation
    {
        public bool VersionCompatible { get; set; }
        public bool StorageAvailable { get; set; }
        public bool NetworkAccessible { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();

    }
}
