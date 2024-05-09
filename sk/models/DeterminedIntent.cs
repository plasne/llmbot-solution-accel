using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.Models.Memory;

public class DeterminedIntent
{
    [JsonProperty("intent", Required = Required.Always)]
    public required Intents Intent { get; set; }

    [JsonProperty("query", Required = Required.Always)]
    public required string Query { get; set; }

    [JsonProperty("search_queries", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public IList<string>? SearchQueries { get; set; }

    [JsonProperty("game_name", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? GameName { get; set; }

    [JsonProperty("edition", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? Edition { get; set; }
}