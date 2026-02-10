using System.Net.Sockets;
using Microsoft.Extensions.Options;
using NetworkScanner.Api.Configuration;
using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public class PortScannerService : IPortScannerService
{
    private readonly ScannerConfiguration _config;
    private readonly ILogger<PortScannerService> _logger;
    private static readonly Dictionary<int, string> CommonServices = new()
    {
        { 21, "FTP" },
        { 22, "SSH" },
        { 23, "Telnet" },
        { 25, "SMTP" },
        { 53, "DNS" },
        { 80, "HTTP" },
        { 110, "POP3" },
        { 143, "IMAP" },
        { 443, "HTTPS" },
        { 445, "SMB" },
        { 3306, "MySQL" },
        { 3389, "RDP" },
        { 5432, "PostgreSQL" },
        { 8080, "HTTP-Alt" },
        { 8443, "HTTPS-Alt" }
    };

    public PortScannerService(
        IOptions<ScannerConfiguration> config,
        ILogger<PortScannerService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<List<NetworkPort>> ScanPortsAsync(
        string ipAddress,
        List<int> ports,
        CancellationToken cancellationToken = default)
    {
        var openPorts = new List<NetworkPort>();
        var semaphore = new SemaphoreSlim(10); // Limit concurrent port scans

        var tasks = ports.Select(async port =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var portInfo = await ScanPortAsync(ipAddress, port, cancellationToken);
                if (portInfo != null && portInfo.State == PortState.Open)
                {
                    lock (openPorts)
                    {
                        openPorts.Add(portInfo);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogDebug("Scanned {PortCount} ports on {IpAddress}, found {OpenPortCount} open",
            ports.Count, ipAddress, openPorts.Count);

        return openPorts.OrderBy(p => p.PortNumber).ToList();
    }

    private async Task<NetworkPort?> ScanPortAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, port);
            var timeoutTask = Task.Delay(_config.PortScanTimeoutMs, cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == connectTask && client.Connected)
            {
                return new NetworkPort
                {
                    PortNumber = port,
                    Protocol = "TCP",
                    ServiceName = CommonServices.GetValueOrDefault(port),
                    State = PortState.Open
                };
            }

            return null;
        }
        catch (SocketException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning port {Port} on {IpAddress}", port, ipAddress);
            return null;
        }
    }
}
