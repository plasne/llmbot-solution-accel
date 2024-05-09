using System.Collections.Generic;
using Newtonsoft.Json;

public class GroundingData
{
    [JsonProperty("docs", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public IList<Doc>? Docs { get; set; }

    [JsonProperty("content", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public IList<Content>? Content { get; set; }

    [JsonProperty("user_query", Required = Required.Always)]
    public required string UserQuery { get; set; }

    [JsonProperty("history", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public IList<Turn>? History { get; set; }
}