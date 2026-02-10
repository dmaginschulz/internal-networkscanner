namespace NetworkScanner.Api.Configuration;

public class ScannerConfiguration
{
    public string NetworkCidr { get; set; } = "192.168.1.0/24";
    public int PingTimeoutMs { get; set; } = 1000;
    public int PortScanTimeoutMs { get; set; } = 500;
    public int MaxConcurrentScans { get; set; } = 50;
    public List<int> CommonPorts { get; set; } = new();
    public int CacheExpirationMinutes { get; set; } = 60;
}
