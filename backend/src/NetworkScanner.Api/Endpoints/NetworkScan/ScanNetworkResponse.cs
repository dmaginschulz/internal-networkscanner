using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Endpoints.NetworkScan;

public class ScanNetworkResponse
{
    public List<Device> Devices { get; set; } = new();
    public int TotalDevicesFound { get; set; }
    public DateTime ScanStartTime { get; set; }
    public DateTime ScanEndTime { get; set; }
    public string NetworkScanned { get; set; } = string.Empty;
}
