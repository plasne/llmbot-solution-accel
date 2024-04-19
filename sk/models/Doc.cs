using System.Text.Json.Serialization;
using Newtonsoft.Json;

public class Doc
{
    [JsonProperty("@search.score")]
    [JsonPropertyName("@search.score")]
    public double SearchScore { get; set; }

    [JsonProperty("chunk_id")]
    [JsonPropertyName("chunk_id")]
    public string? ChunkId { get; set; }

    [JsonProperty("parent_id")]
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    [JsonProperty("chunk")]
    [JsonPropertyName("chunk")]
    public string? Chunk { get; set; }

    [JsonProperty("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonProperty("game_name")]
    [JsonPropertyName("game_name")]
    public string? GameName { get; set; }

    [JsonProperty("edition")]
    [JsonPropertyName("edition")]
    public string? Edition { get; set; }
}