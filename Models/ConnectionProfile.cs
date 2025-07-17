// Models/ConnectionProfile.cs - This should stay the same
using CommunityToolkit.Mvvm.ComponentModel;

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