using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Inference;

public class Doc
{
    [JsonPropertyName("@search.score")]
    [JsonProperty("@search.score")]
    public double SearchScore { get; set; }

    [JsonPropertyName("title")]
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public string? Content { get; set; }

    [JsonPropertyName("url")]
    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
    public string? Url { get; set; }
}