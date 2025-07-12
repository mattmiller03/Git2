namespace UiDesktopApp2.Models
{
    public class ConnectionProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // In production, this should be encrypted
        public bool IsDefault { get; set; }
    }
}