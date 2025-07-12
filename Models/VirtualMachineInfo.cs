using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace UiDesktopApp2.Models
{
    public partial class VirtualMachineInfo : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public string Name { get; set; } = string.Empty;
        public string PowerState { get; set; } = string.Empty;
        public string GuestOS { get; set; } = string.Empty;
        public int CpuCount { get; set; }
        public int MemoryGB { get; set; }
        public int StorageGB { get; set; }
        public string Cluster { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Datastore { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public string ToolsStatus { get; set; } = string.Empty;
        public string VCenter { get; set; } = string.Empty;
        public bool IsMigrationCandidate { get; set; }
    }
}

