using Newtonsoft.Json;

public class Doc
{
    [JsonProperty("@search.score")]
    public double SearchScore { get; set; }

    [JsonProperty("chunk_id")]
    public string? ChunkId { get; set; }

    [JsonProperty("parent_id")]
    public string? ParentId { get; set; }

    [JsonProperty("chunk")]
    public string? Chunk { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("game_name")]
    public string? GameName { get; set; }

    [JsonProperty("edition")]
    public string? Edition { get; set; }
}