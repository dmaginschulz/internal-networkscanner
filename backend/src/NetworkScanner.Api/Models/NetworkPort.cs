namespace NetworkScanner.Api.Models;

public class NetworkPort
{
    public int PortNumber { get; set; }
    public string Protocol { get; set; } = "TCP";
    public string? ServiceName { get; set; }
    public PortState State { get; set; }
}
