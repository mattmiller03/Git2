using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.Models
{
    public class RecentMigration
    {
        public string VmName { get; set; } = string.Empty;
        public string SourceCluster { get; set; } = string.Empty;
        public string DestinationCluster { get; set; } = string.Empty;
        public DateTime MigrationDate { get; set; }
    }
}
