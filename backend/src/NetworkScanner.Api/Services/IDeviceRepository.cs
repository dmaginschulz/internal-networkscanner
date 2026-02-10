using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(string id);
    Task<Device?> GetByIpAddressAsync(string ipAddress);
    Task<List<Device>> GetAllAsync();
    Task AddOrUpdateAsync(Device device);
    Task RemoveAsync(string id);
    Task<int> CountAsync();
}
