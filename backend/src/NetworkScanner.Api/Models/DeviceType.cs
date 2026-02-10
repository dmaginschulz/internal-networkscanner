using System.Text.Json.Serialization;

namespace NetworkScanner.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceType
{
    Unknown,
    Computer,
    Server,
    Router,
    Switch,
    Printer,
    MobileDevice,
    IoTDevice,
    NetworkStorage
}
