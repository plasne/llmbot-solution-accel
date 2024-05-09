using System.Text.Json.Serialization;
using Newtonsoft.Json;

// NOTE: Azure AI Search uses System.Text.Json

public class Doc
{
    [JsonProperty("@search.score", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("@search.score")]
    public double SearchScore { get; set; }

    [JsonProperty("chunk_id", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("chunk_id")]
    public string? ChunkId { get; set; }

    [JsonProperty("chunk", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("chunk")]
    public string? Chunk { get; set; }

    [JsonProperty("title", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}