using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public interface ITopologyDiscoveryService
{
    Task<Dictionary<string, List<string>>> DiscoverPhysicalConnectionsAsync(
        List<Device> devices,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetConnectedDevicesAsync(
        Device device,
        List<Device> allDevices,
        CancellationToken cancellationToken = default);
}
