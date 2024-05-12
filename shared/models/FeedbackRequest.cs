using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class FeedbackRequest
{
    [JsonProperty("rating", NullValueHandling = NullValueHandling.Ignore)]
    public string? Rating { get; set; }

    [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
    public string? Comment { get; set; }
}