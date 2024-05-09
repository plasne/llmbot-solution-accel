using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class ChangeTopicRequest
{
    [JsonProperty("activity_id", Required = Required.Always)]
    public required string ActivityId { get; set; }

    [JsonProperty("intent", Required = Required.Always)]
    public required Intents Intent { get; set; }
}