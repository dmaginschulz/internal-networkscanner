using NetworkScanner.Api.Models;
using System.Text.Json.Serialization;

namespace NetworkScanner.Api.Endpoints.NetworkScan;

public class ScanNetworkResponse
{
    [JsonPropertyName("devices")]
    public List<Device> Devices { get; set; } = new();

    [JsonPropertyName("totalDevicesFound")]
    public int TotalDevicesFound { get; set; }

    [JsonPropertyName("scanStartTime")]
    public DateTime ScanStartTime { get; set; }

    [JsonPropertyName("scanEndTime")]
    public DateTime ScanEndTime { get; set; }

    [JsonPropertyName("networkScanned")]
    public string NetworkScanned { get; set; } = string.Empty;
}
