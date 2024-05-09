using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class StartGenerationRequest
{
    [JsonProperty("request_activity_id", Required = Required.Always)]
    public required string RequestActivityId { get; set; }

    [JsonProperty("query", Required = Required.Always)]
    public required string Query { get; set; }

    [JsonProperty("response_activity_id", Required = Required.Always)]
    public required string ResponseActivityId { get; set; }
}