// Models/ConnectionProfile.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace UiDesktopApp2.Models
{
    public partial class ConnectionProfile : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _serverAddress = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;
    }
}