using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public interface INetworkScannerService
{
    Task<List<Device>> ScanNetworkAsync(string? cidrNotation = null, CancellationToken cancellationToken = default);
    Task<Device?> GetDeviceDetailsAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<List<Device>> GetCachedDevicesAsync();
}
