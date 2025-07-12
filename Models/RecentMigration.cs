using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp2.Models
{
    public class RecentMigration
    {
        public string VmName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
    }
}
