using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public interface IPortScannerService
{
    Task<List<NetworkPort>> ScanPortsAsync(string ipAddress, List<int> ports, CancellationToken cancellationToken = default);
}
