using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Inference;

public class Doc
{
    // NOTE: Azure AI Search uses System.Text.Json

    [JsonProperty("@search.score", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("@search.score")]
    public double SearchScore { get; set; }

    [JsonPropertyName("id")]
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public string? Content { get; set; }

    [JsonPropertyName("url")]
    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
    public string? Url { get; set; }

    [JsonPropertyName("filepath")]
    [JsonProperty("filepath", NullValueHandling = NullValueHandling.Ignore)]
    public string? Filepath { get; set; }

    [JsonPropertyName("meta_json_string")]
    [JsonProperty("meta_json_string", NullValueHandling = NullValueHandling.Ignore)]
    public string? MetaData { get; set; }

    [JsonPropertyName("contentVector")]
    [JsonProperty("contentVector", NullValueHandling = NullValueHandling.Ignore)]
    public float[]? ContentVector { get; set; }
}