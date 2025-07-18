// Models/ConnectionProfile.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public partial class ConnectionProfile : ObservableObject // Change back to ObservableObject for now
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _serverAddress = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    // Add Password back but mark it to be ignored during serialization
    [ObservableProperty]
    [JsonIgnore] // Prevents password from being serialized to JSON
    private string _password = string.Empty;

    // Additional metadata fields (without validation attributes for now)
    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _category = "Default";

    [ObservableProperty]
    private DateTime? _lastConnected = null;
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) &&
               !string.IsNullOrEmpty(ServerAddress) &&
               !string.IsNullOrEmpty(Username);
    }
}
