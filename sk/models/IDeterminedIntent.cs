using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Models.Memory;

public interface IDeterminedIntent
{
    [JsonProperty("intent")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Intents Intent { get; set; }

    [JsonProperty("query")]
    public string Query { get; set; }

    [JsonProperty("search_queries")]
    public List<string>? SearchQueries { get; set; }

    [JsonProperty("game_name")]
    public string? GameName { get; set; }

    [JsonProperty("edition")]
    public string? Edition { get; set; }
}
