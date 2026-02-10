using NetworkScanner.Api.Models;

namespace NetworkScanner.Api.Services;

public interface IInferenceTopologyService
{
    Dictionary<string, List<string>> InferConnections(List<Device> devices);
}
