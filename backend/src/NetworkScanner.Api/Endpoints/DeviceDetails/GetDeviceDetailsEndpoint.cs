using FastEndpoints;
using NetworkScanner.Api.Models;
using NetworkScanner.Api.Services;

namespace NetworkScanner.Api.Endpoints.DeviceDetails;

public class GetDeviceDetailsEndpoint : Endpoint<GetDeviceDetailsRequest, Device>
{
    private readonly INetworkScannerService _scannerService;
    private readonly ILogger<GetDeviceDetailsEndpoint> _logger;

    public GetDeviceDetailsEndpoint(
        INetworkScannerService scannerService,
        ILogger<GetDeviceDetailsEndpoint> logger)
    {
        _scannerService = scannerService;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/devices/{ipAddress}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get device details by IP address";
            s.Description = "Retrieves detailed information about a specific device by its IPv4 or IPv6 address. " +
                          "Performs a fresh scan if the device is not in cache or cache is stale.";
            s.Response<Device>(200, "Device details retrieved successfully");
            s.Response(404, "Device not found or offline");
            s.Response(400, "Invalid IP address format");
        });
    }

    public override async Task HandleAsync(GetDeviceDetailsRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Getting device details for {IpAddress}", req.IpAddress);

        try
        {
            var device = await _scannerService.GetDeviceDetailsAsync(req.IpAddress, ct);

            if (device == null)
            {
                _logger.LogWarning("Device {IpAddress} not found or offline", req.IpAddress);
                await SendNotFoundAsync(ct);
                return;
            }

            await SendAsync(device, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device details for {IpAddress}", req.IpAddress);
            await SendErrorsAsync(500, ct);
        }
    }
}
