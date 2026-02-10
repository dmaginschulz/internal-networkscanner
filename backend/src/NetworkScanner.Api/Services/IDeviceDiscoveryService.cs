using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public interface IDeviceDiscoveryService
{
    Task<string?> GetHostnameAsync(string ipAddress);
    Task<string?> GetMacAddressAsync(string ipAddress);
    DeviceType GuessDeviceType(Device device);
    Task<string?> DetectOperatingSystemAsync(Device device);
    Task<string?> GetDefaultGatewayAsync(string ipAddress);
}
