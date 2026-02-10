using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NetworkScanner.Api.Configuration;
using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public class DeviceRepository : IDeviceRepository
{
    private readonly IMemoryCache _cache;
    private readonly ScannerConfiguration _config;
    private readonly ILogger<DeviceRepository> _logger;
    private const string CacheKeyPrefix = "device_";
    private const string AllDevicesCacheKey = "all_devices";

    public DeviceRepository(
        IMemoryCache cache,
        IOptions<ScannerConfiguration> config,
        ILogger<DeviceRepository> logger)
    {
        _cache = cache;
        _config = config.Value;
        _logger = logger;
    }

    public Task<Device?> GetByIdAsync(string id)
    {
        var cacheKey = CacheKeyPrefix + id;
        _cache.TryGetValue(cacheKey, out Device? device);
        return Task.FromResult(device);
    }

    public async Task<Device?> GetByIpAddressAsync(string ipAddress)
    {
        var allDevices = await GetAllAsync();
        return allDevices.FirstOrDefault(d =>
            d.IPv4Addresses.Contains(ipAddress) ||
            d.IPv6Addresses.Contains(ipAddress));
    }

    public Task<List<Device>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllDevicesCacheKey, out List<Device>? devices))
        {
            return Task.FromResult(devices ?? new List<Device>());
        }

        return Task.FromResult(new List<Device>());
    }

    public async Task AddOrUpdateAsync(Device device)
    {
        var cacheKey = CacheKeyPrefix + device.Id;
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(_config.CacheExpirationMinutes));

        _cache.Set(cacheKey, device, cacheOptions);

        // Update all devices list
        var allDevices = await GetAllAsync();
        var existingDevice = allDevices.FirstOrDefault(d => d.Id == device.Id);

        if (existingDevice != null)
        {
            allDevices.Remove(existingDevice);
        }

        allDevices.Add(device);
        _cache.Set(AllDevicesCacheKey, allDevices, cacheOptions);

        _logger.LogDebug("Device {DeviceId} added/updated in cache", device.Id);
    }

    public async Task RemoveAsync(string id)
    {
        var cacheKey = CacheKeyPrefix + id;
        _cache.Remove(cacheKey);

        var allDevices = await GetAllAsync();
        var deviceToRemove = allDevices.FirstOrDefault(d => d.Id == id);

        if (deviceToRemove != null)
        {
            allDevices.Remove(deviceToRemove);
            _cache.Set(AllDevicesCacheKey, allDevices);
        }

        _logger.LogDebug("Device {DeviceId} removed from cache", id);
    }

    public async Task<int> CountAsync()
    {
        var devices = await GetAllAsync();
        return devices.Count;
    }
}
