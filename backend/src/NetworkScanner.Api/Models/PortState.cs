using System.Text.Json.Serialization;

namespace NetworkScanner.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PortState
{
    Open,
    Closed,
    Filtered
}
