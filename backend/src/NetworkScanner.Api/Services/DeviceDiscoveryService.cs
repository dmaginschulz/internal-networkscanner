using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public class DeviceDiscoveryService : IDeviceDiscoveryService
{
    private readonly ILogger<DeviceDiscoveryService> _logger;

    public DeviceDiscoveryService(ILogger<DeviceDiscoveryService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> GetHostnameAsync(string ipAddress)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
            return hostEntry.HostName;
        }
        catch (SocketException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting hostname for {IpAddress}", ipAddress);
            return null;
        }
    }

    public async Task<string?> GetMacAddressAsync(string ipAddress)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await GetMacAddressWindowsAsync(ipAddress);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await GetMacAddressLinuxAsync(ipAddress);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting MAC address for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<string?> GetMacAddressWindowsAsync(string ipAddress)
    {
        try
        {
            // Use ARP table on Windows
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = $"-a {ipAddress}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(ipAddress))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var macAddress = parts[1];
                        if (macAddress.Contains('-') || macAddress.Contains(':'))
                        {
                            return macAddress.Replace('-', ':').ToUpperInvariant();
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing Windows ARP table for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<string?> GetMacAddressLinuxAsync(string ipAddress)
    {
        try
        {
            // Use ARP table on Linux
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = $"-n {ipAddress}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(ipAddress))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        var macAddress = parts[2];
                        if (macAddress.Contains(':'))
                        {
                            return macAddress.ToUpperInvariant();
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing Linux ARP table for {IpAddress}", ipAddress);
            return null;
        }
    }

    public DeviceType GuessDeviceType(Device device)
    {
        if (device.OpenPorts.Any(p => p.PortNumber == 3389))
            return DeviceType.Computer; // RDP suggests Windows computer

        if (device.OpenPorts.Any(p => p.PortNumber == 22))
        {
            if (device.OpenPorts.Any(p => p.PortNumber == 80 || p.PortNumber == 443))
                return DeviceType.Server; // SSH + HTTP suggests server
        }

        if (device.OpenPorts.Any(p => p.PortNumber == 445))
            return DeviceType.Computer; // SMB suggests Windows computer

        if (device.OpenPorts.Any(p => p.PortNumber == 515 || p.PortNumber == 631 || p.PortNumber == 9100))
            return DeviceType.Printer; // Common printer ports

        if (device.OpenPorts.Any(p => p.PortNumber == 23 || p.PortNumber == 80))
        {
            if (device.Hostname?.ToLower().Contains("router") == true ||
                device.Hostname?.ToLower().Contains("gateway") == true)
                return DeviceType.Router;
        }

        if (device.OpenPorts.Count > 10)
            return DeviceType.Server; // Many open ports suggests server

        return DeviceType.Unknown;
    }

    public Task<string?> DetectOperatingSystemAsync(Device device)
    {
        string? os = null;

        if (device.OpenPorts.Any(p => p.PortNumber == 3389))
            os = "Windows (RDP detected)";
        else if (device.OpenPorts.Any(p => p.PortNumber == 445))
            os = "Windows (SMB detected)";
        else if (device.OpenPorts.Any(p => p.PortNumber == 22))
            os = "Linux/Unix (SSH detected)";

        if (device.Hostname != null)
        {
            if (device.Hostname.ToLower().Contains("windows") || device.Hostname.ToLower().Contains("win"))
                os = "Windows";
            else if (device.Hostname.ToLower().Contains("linux") || device.Hostname.ToLower().Contains("ubuntu") ||
                     device.Hostname.ToLower().Contains("debian") || device.Hostname.ToLower().Contains("centos"))
                os = "Linux";
            else if (device.Hostname.ToLower().Contains("mac") || device.Hostname.ToLower().Contains("apple"))
                os = "macOS";
        }

        return Task.FromResult(os);
    }

    public async Task<string?> GetDefaultGatewayAsync(string ipAddress)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await GetDefaultGatewayWindowsAsync(ipAddress);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await GetDefaultGatewayLinuxAsync();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting default gateway for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<string?> GetDefaultGatewayWindowsAsync(string ipAddress)
    {
        try
        {
            // Parse IP to determine subnet
            var ip = System.Net.IPAddress.Parse(ipAddress);

            // Get all network interfaces
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in networkInterfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                var ipProperties = ni.GetIPProperties();

                // Check if this interface has the IP in its subnet
                foreach (var unicastAddress in ipProperties.UnicastAddresses)
                {
                    if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // Check if IP is in the same subnet
                        var subnetMask = unicastAddress.IPv4Mask;
                        if (IsInSameSubnet(ip, unicastAddress.Address, subnetMask))
                        {
                            // Get gateway for this interface
                            var gateway = ipProperties.GatewayAddresses
                                .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);

                            if (gateway != null)
                            {
                                return gateway.Address.ToString();
                            }
                        }
                    }
                }
            }

            // Fallback: use route print command
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "route",
                    Arguments = "print",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse default gateway from route print output
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("0.0.0.0") && line.Contains("0.0.0.0"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        return parts[2]; // Gateway IP
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting Windows default gateway for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<string?> GetDefaultGatewayLinuxAsync()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ip",
                    Arguments = "route show default",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse: default via 192.168.1.1 dev eth0
            var parts = output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && parts[0] == "default" && parts[1] == "via")
            {
                return parts[2];
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting Linux default gateway");
            return null;
        }
    }

    private bool IsInSameSubnet(IPAddress address1, IPAddress address2, IPAddress subnetMask)
    {
        var ip1Bytes = address1.GetAddressBytes();
        var ip2Bytes = address2.GetAddressBytes();
        var maskBytes = subnetMask.GetAddressBytes();

        for (int i = 0; i < ip1Bytes.Length; i++)
        {
            if ((ip1Bytes[i] & maskBytes[i]) != (ip2Bytes[i] & maskBytes[i]))
            {
                return false;
            }
        }

        return true;
    }
}
