using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.Models
{
    public class DatastoreInfo
    {
        public string Name { get; set; } = string.Empty;
        public double UsedGB { get; set; }
        public double TotalGB { get; set; }
        public double UsagePercentage { get; set; }
    }
}
