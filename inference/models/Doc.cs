using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Inference;

public class Doc
{
    [JsonPropertyName("@search.rerankerScore")]
    [JsonProperty("@search.rerankerScore", NullValueHandling = NullValueHandling.Ignore)]
    public double RerankSearchScore { get; set; }

    [JsonPropertyName("@search.score")]
    [JsonProperty("@search.score")]
    public double SearchScore { get; set; }

    [JsonPropertyName("title")]
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public string? Content { get; set; }

    [JsonPropertyName("urls")]
    [JsonProperty("urls", NullValueHandling = NullValueHandling.Ignore)]
    public string[]? Urls { get; set; }

    [JsonPropertyName("ground_truth_urls")]
    [JsonProperty("ground_truth_urls", NullValueHandling = NullValueHandling.Ignore)]
    public string[]? GroundTruthUrls { get; set; }
}