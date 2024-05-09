using System.Text.Json.Serialization;
using Newtonsoft.Json;

// NOTE: Azure AI Search uses System.Text.Json

public class Doc
{
    [JsonProperty("@search.score", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("@search.score")]
    public double SearchScore { get; set; }

    [JsonProperty("chunk_id", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("chunk_id")]
    public string? ChunkId { get; set; }

    [JsonProperty("chunk", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("chunk")]
    public string? Chunk { get; set; }

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}