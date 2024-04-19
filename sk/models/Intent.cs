using System.Collections.Generic;
using Newtonsoft.Json;

public class Intent
{
    [JsonProperty("query")]
    public string? Query { get; set; }

    [JsonProperty("search_queries")]
    public List<string>? SearchQueries { get; set; }

    [JsonProperty("game_name")]
    public string? GameName { get; set; }

[JsonProperty("edition")]
    public string? Edition { get; set; }

[JsonProperty("action")]
    public string? Action { get; set; }
}
