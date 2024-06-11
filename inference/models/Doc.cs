using Newtonsoft.Json;

namespace Inference;

public class Doc
{
    [JsonProperty("@search.score", NullValueHandling = NullValueHandling.Ignore)]
    public double SearchScore { get; set; }

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public string? Content { get; set; }

    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
    public string? Url { get; set; }
}