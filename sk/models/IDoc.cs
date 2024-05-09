using System.Text.Json.Serialization;
using Newtonsoft.Json;

public interface IDoc
{
    [JsonProperty("@search.score")]
    [JsonPropertyName("@search.score")]
    public double SearchScore { get; set; }

    [JsonProperty("chunk_id")]
    [JsonPropertyName("chunk_id")]
    public string? ChunkId { get; set; }

    [JsonProperty("chunk")]
    [JsonPropertyName("chunk")]
    public string? Chunk { get; set; }

    [JsonProperty("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}