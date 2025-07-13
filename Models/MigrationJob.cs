using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace UiDesktopApp2.Models
{
    public partial class MigrationJob : ObservableObject
    {
        [ObservableProperty]
        private string _vmName = string.Empty;

        [ObservableProperty]
        private string _sourceCluster = string.Empty;

        [ObservableProperty]
        private string _sourceDatastore = string.Empty;

        [ObservableProperty]
        private string _destinationCluster = string.Empty;

        [ObservableProperty]
        private string _destinationDatastore = string.Empty;

        [ObservableProperty]
        private string _migrationType = string.Empty;

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private double _progress = 0;

        [ObservableProperty]
        private DateTime? _startTime;

        [ObservableProperty]
        private DateTime? _endTime;

        public DateTime QueuedTime { get; set; }
        public bool PowerOffBeforeMigration { get; set; }
        public bool CreateSnapshot { get; set; }
        public bool ValidateCompatibility { get; set; }
    }
}
