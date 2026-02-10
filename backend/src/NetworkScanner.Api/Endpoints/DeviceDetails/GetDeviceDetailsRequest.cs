using FastEndpoints;
using FluentValidation;
using System.Net;

namespace NetworkScanner.Api.Endpoints.DeviceDetails;

public class GetDeviceDetailsRequest
{
    public string IpAddress { get; set; } = string.Empty;
}

public class GetDeviceDetailsValidator : Validator<GetDeviceDetailsRequest>
{
    public GetDeviceDetailsValidator()
    {
        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .WithMessage("IP address is required")
            .Must(BeValidIpAddress)
            .WithMessage("Must be a valid IPv4 or IPv6 address");
    }

    private bool BeValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }
}
