using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class ClearFeedbackRequest
{
    [JsonProperty("activity_id", Required = Required.Always)]
    public required string ActivityId { get; set; }
}