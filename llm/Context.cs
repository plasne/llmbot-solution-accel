using System.Text.Json.Serialization;

public class Context
{
    [JsonPropertyName("@search.score")]
    public double SearchScore { get; set; }

    [JsonPropertyName("chunk_id")]
    public string? ChunkId { get; set; }

    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    [JsonPropertyName("chunk")]
    public string? Chunk { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("game_name")]
    public string? GameName { get; set; }

    [JsonPropertyName("edition")]
    public string? Edition { get; set; }
}