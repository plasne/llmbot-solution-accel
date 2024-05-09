using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class FeedbackRequest
{
    [JsonProperty("activity_id", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? ActivityId { get; set; }

    [JsonProperty("rating", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? Rating { get; set; }

    [JsonProperty("comment", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? Comment { get; set; }
}