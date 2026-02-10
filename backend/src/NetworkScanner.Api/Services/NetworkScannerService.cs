using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;
using NetworkScanner.Api.Configuration;
using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public class NetworkScannerService : INetworkScannerService
{
    private readonly ScannerConfiguration _config;
    private readonly IPortScannerService _portScanner;
    private readonly IDeviceDiscoveryService _deviceDiscovery;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<NetworkScannerService> _logger;

    public NetworkScannerService(
        IOptions<ScannerConfiguration> config,
        IPortScannerService portScanner,
        IDeviceDiscoveryService deviceDiscovery,
        IDeviceRepository deviceRepository,
        ILogger<NetworkScannerService> logger)
    {
        _config = config.Value;
        _portScanner = portScanner;
        _deviceDiscovery = deviceDiscovery;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public async Task<List<Device>> ScanNetworkAsync(
        string? cidrNotation = null,
        CancellationToken cancellationToken = default)
    {
        var cidr = cidrNotation ?? _config.NetworkCidr;
        _logger.LogInformation("Starting network scan for {Cidr}", cidr);

        var ipAddresses = cidr.Contains('-')
            ? ParseIpRange(cidr)
            : ParseCidr(cidr);
        _logger.LogInformation("Scanning {Count} IP addresses", ipAddresses.Count);

        var devices = new List<Device>();
        var semaphore = new SemaphoreSlim(_config.MaxConcurrentScans);

        var tasks = ipAddresses.Select(async ip =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var device = await ScanDeviceAsync(ip, cancellationToken);
                if (device != null)
                {
                    lock (devices)
                    {
                        devices.Add(device);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // Save all devices to repository
        foreach (var device in devices)
        {
            await _deviceRepository.AddOrUpdateAsync(device);
        }

        _logger.LogInformation("Network scan completed. Found {DeviceCount} devices", devices.Count);

        return devices.OrderBy(d => d.IPv4Addresses.FirstOrDefault()).ToList();
    }

    public async Task<Device?> GetDeviceDetailsAsync(
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting device details for {IpAddress}", ipAddress);

        // Check cache first
        var cachedDevice = await _deviceRepository.GetByIpAddressAsync(ipAddress);
        if (cachedDevice != null && (DateTime.UtcNow - cachedDevice.LastSeen).TotalMinutes < 5)
        {
            _logger.LogDebug("Returning cached device for {IpAddress}", ipAddress);
            return cachedDevice;
        }

        // Perform fresh scan
        var device = await ScanDeviceAsync(ipAddress, cancellationToken);
        if (device != null)
        {
            await _deviceRepository.AddOrUpdateAsync(device);
        }

        return device;
    }

    public async Task<List<Device>> GetCachedDevicesAsync()
    {
        return await _deviceRepository.GetAllAsync();
    }

    private async Task<Device?> ScanDeviceAsync(string ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            // Check if device is online with ping
            var isOnline = await PingAsync(ipAddress, cancellationToken);
            if (!isOnline)
            {
                return null;
            }

            _logger.LogDebug("Device {IpAddress} is online, gathering details", ipAddress);

            var device = new Device
            {
                Id = ipAddress.Replace(":", "_").Replace(".", "_"),
                IsOnline = true,
                LastSeen = DateTime.UtcNow,
                FirstDiscovered = DateTime.UtcNow
            };

            // Determine if IPv4 or IPv6
            if (IPAddress.TryParse(ipAddress, out var parsedIp))
            {
                if (parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    device.IPv4Addresses.Add(ipAddress);
                }
                else if (parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    device.IPv6Addresses.Add(ipAddress);
                }
            }

            // Get hostname
            device.Hostname = await _deviceDiscovery.GetHostnameAsync(ipAddress);

            // Scan common ports
            device.OpenPorts = await _portScanner.ScanPortsAsync(
                ipAddress,
                _config.CommonPorts,
                cancellationToken);

            // Get MAC address
            device.MacAddress = await _deviceDiscovery.GetMacAddressAsync(ipAddress);

            // Guess device type
            device.DeviceType = _deviceDiscovery.GuessDeviceType(device);

            // Detect OS
            device.OperatingSystem = await _deviceDiscovery.DetectOperatingSystemAsync(device);

            _logger.LogInformation("Successfully scanned device {IpAddress} ({Hostname})",
                ipAddress, device.Hostname ?? "Unknown");

            return device;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning device {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<bool> PingAsync(string ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, _config.PingTimeoutMs);
            return reply.Status == IPStatus.Success;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ping failed for {IpAddress}", ipAddress);
            return false;
        }
    }

    private List<string> ParseCidr(string cidr)
    {
        var ipAddresses = new List<string>();

        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid CIDR notation: {Cidr}", cidr);
                return ipAddresses;
            }

            var ipParts = parts[0].Split('.');
            if (ipParts.Length != 4)
            {
                _logger.LogWarning("Invalid IP address in CIDR: {Cidr}", cidr);
                return ipAddresses;
            }

            var subnet = int.Parse(parts[1]);
            var hostBits = 32 - subnet;
            var numberOfHosts = (int)Math.Pow(2, hostBits) - 2; // Exclude network and broadcast

            var baseIp = IPAddress.Parse(parts[0]);
            var baseIpBytes = baseIp.GetAddressBytes();
            var baseIpInt = BitConverter.ToUInt32(baseIpBytes.Reverse().ToArray(), 0);

            // Generate all IPs in range
            for (int i = 1; i <= numberOfHosts; i++)
            {
                var ipInt = baseIpInt + (uint)i;
                var ipBytes = BitConverter.GetBytes(ipInt).Reverse().ToArray();
                var ip = new IPAddress(ipBytes);
                ipAddresses.Add(ip.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing CIDR notation: {Cidr}", cidr);
        }

        return ipAddresses;
    }

    private List<string> ParseIpRange(string ipRange)
    {
        var ipAddresses = new List<string>();

        try
        {
            var parts = ipRange.Split('-');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid IP range format: {IpRange}. Expected format: 192.168.1.1-192.168.1.254", ipRange);
                return ipAddresses;
            }

            var startIp = parts[0].Trim();
            var endIp = parts[1].Trim();

            if (!IPAddress.TryParse(startIp, out var startAddress) ||
                !IPAddress.TryParse(endIp, out var endAddress))
            {
                _logger.LogWarning("Invalid IP addresses in range: {IpRange}", ipRange);
                return ipAddresses;
            }

            // Only support IPv4 ranges for now
            if (startAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                endAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                _logger.LogWarning("Only IPv4 ranges are supported: {IpRange}", ipRange);
                return ipAddresses;
            }

            var startBytes = startAddress.GetAddressBytes();
            var endBytes = endAddress.GetAddressBytes();

            var startInt = BitConverter.ToUInt32(startBytes.Reverse().ToArray(), 0);
            var endInt = BitConverter.ToUInt32(endBytes.Reverse().ToArray(), 0);

            if (startInt > endInt)
            {
                _logger.LogWarning("Start IP is greater than end IP: {IpRange}", ipRange);
                return ipAddresses;
            }

            var totalIps = endInt - startInt + 1;
            if (totalIps > 65536) // Limit to /16 network max
            {
                _logger.LogWarning("IP range too large (max 65536 IPs): {IpRange}", ipRange);
                return ipAddresses;
            }

            // Generate all IPs in range
            for (uint i = startInt; i <= endInt; i++)
            {
                var ipBytes = BitConverter.GetBytes(i).Reverse().ToArray();
                var ip = new IPAddress(ipBytes);
                ipAddresses.Add(ip.ToString());
            }

            _logger.LogInformation("Parsed IP range {IpRange} to {Count} addresses", ipRange, ipAddresses.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing IP range: {IpRange}", ipRange);
        }

        return ipAddresses;
    }
}
