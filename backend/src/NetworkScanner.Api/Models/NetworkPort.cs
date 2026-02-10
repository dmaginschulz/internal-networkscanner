using System.Text.Json.Serialization;

namespace NetworkScanner.Api.Models;

public class NetworkPort
{
    [JsonPropertyName("portNumber")]
    public int PortNumber { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "TCP";

    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("state")]
    public PortState State { get; set; }
}
