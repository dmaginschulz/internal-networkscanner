using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public class InferenceTopologyService : IInferenceTopologyService
{
    private readonly ILogger<InferenceTopologyService> _logger;

    public InferenceTopologyService(ILogger<InferenceTopologyService> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, List<string>> InferConnections(List<Device> devices)
    {
        _logger.LogInformation("Starting inference-based topology discovery for {DeviceCount} devices", devices.Count);

        var connections = new Dictionary<string, List<string>>();

        // Initialize connections for all devices
        foreach (var device in devices)
        {
            connections[device.Id] = new List<string>();
        }

        // Method 1: Subnet-based inference
        InferBySubnet(devices, connections);

        // Method 2: Gateway-based clustering
        InferByGateway(devices, connections);

        // Method 3: Port pattern analysis
        InferByPortPatterns(devices, connections);

        // Method 4: Hostname pattern matching
        InferByHostnamePatterns(devices, connections);

        // Method 5: MAC address vendor analysis
        InferByMacVendor(devices, connections);

        _logger.LogInformation("Inference-based topology discovery completed. Inferred {ConnectionCount} connections",
            connections.Values.Sum(c => c.Count));

        return connections;
    }

    private void InferBySubnet(List<Device> devices, Dictionary<string, List<string>> connections)
    {
        // Group devices by subnet
        var devicesBySubnet = devices
            .Where(d => d.IPv4Addresses.Count > 0)
            .GroupBy(d => GetSubnet(d.IPv4Addresses[0]))
            .ToList();

        foreach (var subnetGroup in devicesBySubnet)
        {
            var subnetDevices = subnetGroup.ToList();

            // Find infrastructure devices in this subnet
            var infrastructure = subnetDevices.Where(d =>
                d.DeviceType == DeviceType.Router ||
                d.DeviceType == DeviceType.Switch).ToList();

            if (infrastructure.Count == 0)
                continue;

            // Connect all non-infrastructure devices to the first infrastructure device
            var mainInfra = infrastructure.First();

            foreach (var device in subnetDevices.Where(d =>
                d.DeviceType != DeviceType.Router &&
                d.DeviceType != DeviceType.Switch))
            {
                if (!connections[mainInfra.Id].Contains(device.Id))
                {
                    connections[mainInfra.Id].Add(device.Id);
                }

                if (!connections[device.Id].Contains(mainInfra.Id))
                {
                    connections[device.Id].Add(mainInfra.Id);
                }
            }

            // Connect infrastructure devices to each other
            for (int i = 0; i < infrastructure.Count - 1; i++)
            {
                for (int j = i + 1; j < infrastructure.Count; j++)
                {
                    if (!connections[infrastructure[i].Id].Contains(infrastructure[j].Id))
                    {
                        connections[infrastructure[i].Id].Add(infrastructure[j].Id);
                    }

                    if (!connections[infrastructure[j].Id].Contains(infrastructure[i].Id))
                    {
                        connections[infrastructure[j].Id].Add(infrastructure[i].Id);
                    }
                }
            }
        }

        _logger.LogDebug("Subnet-based inference: Added connections");
    }

    private void InferByGateway(List<Device> devices, Dictionary<string, List<string>> connections)
    {
        // Connect devices that share the same gateway
        var devicesByGateway = devices
            .Where(d => d.DefaultGateway != null)
            .GroupBy(d => d.DefaultGateway!)
            .ToList();

        foreach (var gatewayGroup in devicesByGateway)
        {
            var gateway = gatewayGroup.Key;
            var gatewayDevice = devices.FirstOrDefault(d => d.IPv4Addresses.Contains(gateway));

            if (gatewayDevice == null)
                continue;

            foreach (var device in gatewayGroup)
            {
                if (device.Id == gatewayDevice.Id)
                    continue;

                if (!connections[gatewayDevice.Id].Contains(device.Id))
                {
                    connections[gatewayDevice.Id].Add(device.Id);
                }

                if (!connections[device.Id].Contains(gatewayDevice.Id))
                {
                    connections[device.Id].Add(gatewayDevice.Id);
                }
            }
        }

        _logger.LogDebug("Gateway-based inference: Added connections");
    }

    private void InferByPortPatterns(List<Device> devices, Dictionary<string, List<string>> connections)
    {
        // Servers with many open ports are likely connected to infrastructure
        var servers = devices.Where(d =>
            d.DeviceType == DeviceType.Server ||
            d.OpenPorts.Count > 5).ToList();

        var infrastructure = devices.Where(d =>
            d.DeviceType == DeviceType.Router ||
            d.DeviceType == DeviceType.Switch).ToList();

        foreach (var server in servers)
        {
            // Find closest infrastructure device (same subnet)
            var subnet = server.IPv4Addresses.Count > 0 ? GetSubnet(server.IPv4Addresses[0]) : null;
            if (subnet == null)
                continue;

            var closestInfra = infrastructure.FirstOrDefault(infra =>
                infra.IPv4Addresses.Count > 0 &&
                GetSubnet(infra.IPv4Addresses[0]) == subnet);

            if (closestInfra != null)
            {
                if (!connections[closestInfra.Id].Contains(server.Id))
                {
                    connections[closestInfra.Id].Add(server.Id);
                }

                if (!connections[server.Id].Contains(closestInfra.Id))
                {
                    connections[server.Id].Add(closestInfra.Id);
                }
            }
        }

        _logger.LogDebug("Port pattern inference: Added connections");
    }

    private void InferByHostnamePatterns(List<Device> devices, Dictionary<string, List<string>> connections)
    {
        // Connect devices with similar hostname patterns
        var devicesWithHostnames = devices.Where(d => d.Hostname != null).ToList();

        foreach (var device in devicesWithHostnames)
        {
            var hostname = device.Hostname!.ToLower();

            // If hostname contains location/department identifiers, connect to switches with same pattern
            var baseName = ExtractBasePattern(hostname);

            if (string.IsNullOrEmpty(baseName))
                continue;

            var relatedDevices = devicesWithHostnames.Where(d =>
                d.Id != device.Id &&
                ExtractBasePattern(d.Hostname!.ToLower()) == baseName &&
                (d.DeviceType == DeviceType.Switch || d.DeviceType == DeviceType.Router)).ToList();

            foreach (var related in relatedDevices)
            {
                if (!connections[device.Id].Contains(related.Id))
                {
                    connections[device.Id].Add(related.Id);
                }

                if (!connections[related.Id].Contains(device.Id))
                {
                    connections[related.Id].Add(device.Id);
                }
            }
        }

        _logger.LogDebug("Hostname pattern inference: Added connections");
    }

    private void InferByMacVendor(List<Device> devices, Dictionary<string, List<string>> connections)
    {
        // Devices from same vendor (based on MAC address) might be connected to vendor-specific infrastructure
        var devicesWithMac = devices.Where(d => d.MacAddress != null).ToList();

        var vendorGroups = devicesWithMac
            .GroupBy(d => GetMacVendorPrefix(d.MacAddress!))
            .Where(g => g.Count() > 2) // Only consider if there are multiple devices from same vendor
            .ToList();

        foreach (var vendorGroup in vendorGroups)
        {
            var vendorDevices = vendorGroup.ToList();

            // Find infrastructure devices in this vendor group
            var vendorInfra = vendorDevices.Where(d =>
                d.DeviceType == DeviceType.Router ||
                d.DeviceType == DeviceType.Switch).ToList();

            if (vendorInfra.Count == 0)
                continue;

            // Connect non-infrastructure devices to vendor infrastructure
            foreach (var device in vendorDevices.Where(d =>
                d.DeviceType != DeviceType.Router &&
                d.DeviceType != DeviceType.Switch))
            {
                foreach (var infra in vendorInfra)
                {
                    if (!connections[infra.Id].Contains(device.Id))
                    {
                        connections[infra.Id].Add(device.Id);
                    }

                    if (!connections[device.Id].Contains(infra.Id))
                    {
                        connections[device.Id].Add(infra.Id);
                    }
                }
            }
        }

        _logger.LogDebug("MAC vendor inference: Added connections");
    }

    private string GetSubnet(string ipAddress)
    {
        var parts = ipAddress.Split('.');
        return parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : "";
    }

    private string ExtractBasePattern(string hostname)
    {
        // Extract base pattern from hostname (e.g., "office-pc-01" -> "office")
        // Common patterns: location-type-number, building-floor-room, etc.

        var parts = hostname.Split(new[] { '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return "";

        // Return first non-numeric part
        return parts.FirstOrDefault(p => !int.TryParse(p, out _)) ?? "";
    }

    private string GetMacVendorPrefix(string macAddress)
    {
        // Get first 3 bytes (OUI - Organizationally Unique Identifier)
        var parts = macAddress.Split(new[] { ':', '-' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 3)
        {
            return $"{parts[0]}-{parts[1]}-{parts[2]}".ToUpperInvariant();
        }

        return "";
    }
}
