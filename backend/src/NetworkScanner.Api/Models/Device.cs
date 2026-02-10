namespace NetworkScanner.Api.Models;

public class Device
{
    public string Id { get; set; } = string.Empty;
    public List<string> IPv4Addresses { get; set; } = new();
    public List<string> IPv6Addresses { get; set; } = new();
    public string? Hostname { get; set; }
    public string? MacAddress { get; set; }
    public List<NetworkPort> OpenPorts { get; set; } = new();
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
    public string? OperatingSystem { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DateTime FirstDiscovered { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
}
