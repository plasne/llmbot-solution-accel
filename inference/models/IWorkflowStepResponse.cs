using Newtonsoft.Json;

namespace Inference;

public interface IWorkflowStepResponse
{
    [JsonProperty("name", Required = Required.Always)]
    string Name { get; set; }

    [JsonProperty("usage", Required = Required.Always)]
    Usage Usage { get; set; }
}