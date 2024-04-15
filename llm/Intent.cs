using System.Collections.Generic;
using Newtonsoft.Json;

public class Intent
{
    public string? Query { get; set; }

    [JsonProperty("search_queries")]
    public List<string>? SearchQueries { get; set; }

    [JsonProperty("game_name")]
    public string? GameName { get; set; }

    public string? Edition { get; set; }

    public string? Action { get; set; }
}