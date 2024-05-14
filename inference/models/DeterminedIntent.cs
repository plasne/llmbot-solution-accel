using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Models.Memory;

namespace Inference;

public class DeterminedIntent
{
    [JsonProperty("intent", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public required Intents Intent { get; set; }

    [JsonProperty("query", Required = Required.Always)]
    public required string Query { get; set; }

    [JsonProperty("search_queries", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? SearchQueries { get; set; }

    [JsonProperty("game_name", NullValueHandling = NullValueHandling.Ignore)]
    public string? GameName { get; set; }

    [JsonProperty("edition", NullValueHandling = NullValueHandling.Ignore)]
    public string? Edition { get; set; }
}