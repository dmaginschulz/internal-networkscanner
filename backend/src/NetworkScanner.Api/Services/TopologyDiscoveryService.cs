using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public class TopologyDiscoveryService : ITopologyDiscoveryService
{
    private readonly ILogger<TopologyDiscoveryService> _logger;

    // SNMP OIDs for LLDP
    private const string LLDP_REM_SYS_NAME = "1.0.8802.1.1.2.1.4.1.1.9";
    private const string LLDP_REM_PORT_ID = "1.0.8802.1.1.2.1.4.1.1.7";
    private const string LLDP_LOC_PORT_ID = "1.0.8802.1.1.2.1.3.7.1.3";

    // SNMP OIDs for bridge MIB (MAC address table)
    private const string DOT1D_TP_FDB_PORT = "1.3.6.1.2.1.17.4.3.1.2";
    private const string DOT1D_BASE_PORT_IF_INDEX = "1.3.6.1.2.1.17.1.4.1.2";

    // SNMP OIDs for CDP (Cisco Discovery Protocol)
    private const string CDP_CACHE_ADDRESS = "1.3.6.1.4.1.9.9.23.1.2.1.1.4";
    private const string CDP_CACHE_DEVICE_ID = "1.3.6.1.4.1.9.9.23.1.2.1.1.6";
    private const string CDP_CACHE_DEVICE_PORT = "1.3.6.1.4.1.9.9.23.1.2.1.1.7";

    public TopologyDiscoveryService(ILogger<TopologyDiscoveryService> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, List<string>>> DiscoverPhysicalConnectionsAsync(
        List<Device> devices,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting physical topology discovery for {DeviceCount} devices", devices.Count);

        var connections = new Dictionary<string, List<string>>();

        // Initialize connections for all devices
        foreach (var device in devices)
        {
            connections[device.Id] = new List<string>();
        }

        // Find switches and routers that might provide topology info
        var networkInfrastructure = devices.Where(d =>
            d.DeviceType == DeviceType.Router ||
            d.DeviceType == DeviceType.Switch).ToList();

        _logger.LogInformation("Found {InfrastructureCount} infrastructure devices", networkInfrastructure.Count);

        foreach (var infraDevice in networkInfrastructure)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var deviceConnections = await GetConnectedDevicesAsync(infraDevice, devices, cancellationToken);

                foreach (var connectedDeviceId in deviceConnections)
                {
                    if (!connections[infraDevice.Id].Contains(connectedDeviceId))
                    {
                        connections[infraDevice.Id].Add(connectedDeviceId);
                    }

                    // Add reverse connection
                    var connectedDevice = devices.FirstOrDefault(d => d.Id == connectedDeviceId);
                    if (connectedDevice != null && !connections[connectedDeviceId].Contains(infraDevice.Id))
                    {
                        connections[connectedDeviceId].Add(infraDevice.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error discovering connections for {DeviceId}", infraDevice.Id);
            }
        }

        _logger.LogInformation("Physical topology discovery completed. Found {ConnectionCount} connections",
            connections.Values.Sum(c => c.Count));

        return connections;
    }

    public async Task<List<string>> GetConnectedDevicesAsync(
        Device device,
        List<Device> allDevices,
        CancellationToken cancellationToken = default)
    {
        var connectedDevices = new List<string>();

        if (device.IPv4Addresses.Count == 0)
        {
            return connectedDevices;
        }

        var ipAddress = device.IPv4Addresses[0];

        // Try LLDP first
        var lldpConnections = await TryGetLldpConnectionsAsync(ipAddress, allDevices, cancellationToken);
        connectedDevices.AddRange(lldpConnections);

        // Try CDP if LLDP didn't work (for Cisco devices)
        if (connectedDevices.Count == 0)
        {
            var cdpConnections = await TryGetCdpConnectionsAsync(ipAddress, allDevices, cancellationToken);
            connectedDevices.AddRange(cdpConnections);
        }

        // Try MAC address table as fallback
        if (connectedDevices.Count == 0)
        {
            var macTableConnections = await TryGetMacTableConnectionsAsync(ipAddress, allDevices, cancellationToken);
            connectedDevices.AddRange(macTableConnections);
        }

        return connectedDevices.Distinct().ToList();
    }

    private async Task<List<string>> TryGetLldpConnectionsAsync(
        string ipAddress,
        List<Device> allDevices,
        CancellationToken cancellationToken)
    {
        var connectedDevices = new List<string>();

        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), 161);
            var community = new OctetString("public"); // Default SNMP community

            // Walk the LLDP remote system name table
            var results = new List<Variable>();

            await Task.Run(() =>
            {
                try
                {
                    var walked = Messenger.Walk(
                        VersionCode.V2,
                        endpoint,
                        community,
                        new ObjectIdentifier(LLDP_REM_SYS_NAME),
                        results,
                        2000, // 2 second timeout
                        WalkMode.WithinSubtree);
                }
                catch
                {
                    // SNMP walk failed
                }
            }, cancellationToken);

            foreach (var result in results)
            {
                var remoteSystemName = result.Data.ToString();

                // Try to find device by hostname
                var matchedDevice = allDevices.FirstOrDefault(d =>
                    d.Hostname != null &&
                    d.Hostname.Equals(remoteSystemName, StringComparison.OrdinalIgnoreCase));

                if (matchedDevice != null)
                {
                    connectedDevices.Add(matchedDevice.Id);
                    _logger.LogDebug("LLDP: Found connection to {RemoteSystem}", remoteSystemName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLDP query failed for {IpAddress}", ipAddress);
        }

        return connectedDevices;
    }

    private async Task<List<string>> TryGetCdpConnectionsAsync(
        string ipAddress,
        List<Device> allDevices,
        CancellationToken cancellationToken)
    {
        var connectedDevices = new List<string>();

        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), 161);
            var community = new OctetString("public");

            var results = new List<Variable>();

            await Task.Run(() =>
            {
                try
                {
                    var walked = Messenger.Walk(
                        VersionCode.V2,
                        endpoint,
                        community,
                        new ObjectIdentifier(CDP_CACHE_DEVICE_ID),
                        results,
                        2000,
                        WalkMode.WithinSubtree);
                }
                catch
                {
                    // SNMP walk failed
                }
            }, cancellationToken);

            foreach (var result in results)
            {
                var remoteDeviceId = result.Data.ToString();

                // Try to find device by hostname
                var matchedDevice = allDevices.FirstOrDefault(d =>
                    d.Hostname != null &&
                    d.Hostname.Contains(remoteDeviceId, StringComparison.OrdinalIgnoreCase));

                if (matchedDevice != null)
                {
                    connectedDevices.Add(matchedDevice.Id);
                    _logger.LogDebug("CDP: Found connection to {RemoteDevice}", remoteDeviceId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CDP query failed for {IpAddress}", ipAddress);
        }

        return connectedDevices;
    }

    private async Task<List<string>> TryGetMacTableConnectionsAsync(
        string ipAddress,
        List<Device> allDevices,
        CancellationToken cancellationToken)
    {
        var connectedDevices = new List<string>();

        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), 161);
            var community = new OctetString("public");

            var results = new List<Variable>();

            await Task.Run(() =>
            {
                try
                {
                    var walked = Messenger.Walk(
                        VersionCode.V2,
                        endpoint,
                        community,
                        new ObjectIdentifier(DOT1D_TP_FDB_PORT),
                        results,
                        2000,
                        WalkMode.WithinSubtree);
                }
                catch
                {
                    // SNMP walk failed
                }
            }, cancellationToken);

            // Parse MAC addresses from OID and match with devices
            foreach (var result in results)
            {
                try
                {
                    var oid = result.Id.ToString();
                    var parts = oid.Split('.');

                    if (parts.Length >= 6)
                    {
                        // Last 6 parts of OID are MAC address bytes
                        var macBytes = parts.Skip(parts.Length - 6).Select(byte.Parse).ToArray();
                        var macAddress = string.Join(":", macBytes.Select(b => b.ToString("X2")));

                        // Find device with this MAC address
                        var matchedDevice = allDevices.FirstOrDefault(d =>
                            d.MacAddress != null &&
                            d.MacAddress.Replace("-", ":").Equals(macAddress, StringComparison.OrdinalIgnoreCase));

                        if (matchedDevice != null)
                        {
                            connectedDevices.Add(matchedDevice.Id);
                            _logger.LogDebug("MAC Table: Found connection to device with MAC {MacAddress}", macAddress);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error parsing MAC table entry");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "MAC table query failed for {IpAddress}", ipAddress);
        }

        return connectedDevices;
    }
}
