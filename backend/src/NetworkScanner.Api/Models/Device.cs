using System.Text.Json.Serialization;

namespace NetworkScanner.Api.Models;

public class Device
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("ipv4Addresses")]
    public List<string> IPv4Addresses { get; set; } = new();

    [JsonPropertyName("ipv6Addresses")]
    public List<string> IPv6Addresses { get; set; } = new();

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("openPorts")]
    public List<NetworkPort> OpenPorts { get; set; } = new();

    [JsonPropertyName("deviceType")]
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    [JsonPropertyName("operatingSystem")]
    public string? OperatingSystem { get; set; }

    [JsonPropertyName("lastSeen")]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("firstDiscovered")]
    public DateTime FirstDiscovered { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }

    [JsonPropertyName("defaultGateway")]
    public string? DefaultGateway { get; set; }

    [JsonPropertyName("connectedTo")]
    public List<string> ConnectedTo { get; set; } = new();
}
