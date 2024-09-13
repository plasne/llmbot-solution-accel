using Newtonsoft.Json;

namespace Inference;

public class LlmConnectionDetails
{
    [JsonProperty(nameof(DeploymentName), Required = Required.Always)]
    public required string DeploymentName { get; set; }

    [JsonProperty(nameof(Endpoint), Required = Required.Always)]
    public required string Endpoint { get; set; }

    [JsonProperty(nameof(ApiKey), Required = Required.Always)]
    [JsonConverter(typeof(JsonMaskConverter))]
    public required string ApiKey { get; set; }
}