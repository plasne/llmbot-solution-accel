using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public enum Intents
{
    UNKNOWN = 0,
    GREETING = 1,
    GOODBYE = 2,
    IN_DOMAIN = 3,
    OUT_OF_DOMAIN = 4,
    TOPIC_CHANGE = 5,
}

public class DeterminedIntent
{
    [JsonProperty("intent")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Intents Intent { get; set; }

    [JsonProperty("query")]
    public string? Query { get; set; }

    [JsonProperty("search_queries")]
    public List<string>? SearchQueries { get; set; }

    [JsonProperty("game_name")]
    public string? GameName { get; set; }

    [JsonProperty("edition")]
    public string? Edition { get; set; }
}
