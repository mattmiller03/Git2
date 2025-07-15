using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.Models
{
    public record ConnectionResult
    {
        public bool IsConnected { get; set; }
        public string? Version { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsSuccessful { get; set; }

        public ConnectionResult(bool isSuccessful, string? version = null, string? errorMessage = null)
        {
            IsSuccessful = isSuccessful;
            Version = version;
            ErrorMessage = errorMessage;
        }

    }
}
