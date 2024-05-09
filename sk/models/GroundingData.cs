using System.Collections.Generic;
using Newtonsoft.Json;

public class GroundingData
{
    [JsonProperty("docs", NullValueHandling = NullValueHandling.Ignore)]
    public IList<Doc>? Docs { get; set; }

    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public IList<Content>? Content { get; set; }

    [JsonProperty("user_query")]
    public required string UserQuery { get; set; }

    [JsonProperty("history", NullValueHandling = NullValueHandling.Ignore)]
    public IList<Turn>? History { get; set; }
}