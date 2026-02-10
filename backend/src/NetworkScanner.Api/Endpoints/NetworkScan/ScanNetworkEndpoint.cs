using FastEndpoints;
using NetworkScanner.Api.Services;

namespace NetworkScanner.Api.Endpoints.NetworkScan;

public class ScanNetworkEndpoint : EndpointWithoutRequest<ScanNetworkResponse>
{
    private readonly INetworkScannerService _scannerService;
    private readonly ILogger<ScanNetworkEndpoint> _logger;

    public ScanNetworkEndpoint(
        INetworkScannerService scannerService,
        ILogger<ScanNetworkEndpoint> logger)
    {
        _scannerService = scannerService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/scan");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Scan network for devices";
            s.Description = "Initiates a network scan and returns discovered devices with their details. " +
                          "Optionally accepts a CIDR notation parameter to specify the network range.";
            s.Response<ScanNetworkResponse>(200, "Network scan completed successfully");
            s.Response(500, "Internal server error during scan");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var cidr = Query<string>("cidr", isRequired: false);
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Starting network scan request. CIDR: {Cidr}", cidr ?? "default");

        try
        {
            var devices = await _scannerService.ScanNetworkAsync(cidr, ct);

            var response = new ScanNetworkResponse
            {
                Devices = devices,
                TotalDevicesFound = devices.Count,
                ScanStartTime = startTime,
                ScanEndTime = DateTime.UtcNow,
                NetworkScanned = cidr ?? "Default network from configuration"
            };

            _logger.LogInformation("Network scan completed. Found {DeviceCount} devices in {Duration}s",
                devices.Count, (response.ScanEndTime - response.ScanStartTime).TotalSeconds);

            await SendAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during network scan");
            await SendErrorsAsync(500, ct);
        }
    }
}
