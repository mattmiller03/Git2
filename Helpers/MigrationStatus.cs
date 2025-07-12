using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp2.Helpers
{
    public enum MigrationStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Queued
    }
}
