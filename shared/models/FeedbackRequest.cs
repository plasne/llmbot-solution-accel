using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class FeedbackRequest
{
    [JsonProperty("activity_id", Required = Required.AllowNull)]
    public string? ActivityId { get; set; }

    [JsonProperty("rating", Required = Required.AllowNull)]
    public string? Rating { get; set; }

    [JsonProperty("comment", Required = Required.AllowNull)]
    public string? Comment { get; set; }
}