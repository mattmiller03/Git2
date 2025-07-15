using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Services;


using CommunityToolkit.Mvvm.ComponentModel;

namespace UiDesktopApp2.Models
{
    public partial class ActiveMigration : ObservableObject
    {
        [ObservableProperty]
        private string _vmName = string.Empty;

        [ObservableProperty]
        private string _migrationType = string.Empty;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private string _timeRemaining = string.Empty;
    }
}
