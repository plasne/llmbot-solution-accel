using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class FeedbackRequest
{
    [JsonProperty("activity_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? ActivityId { get; set; }

    [JsonProperty("rating", NullValueHandling = NullValueHandling.Ignore)]
    public string? Rating { get; set; }

    [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
    public string? Comment { get; set; }
}