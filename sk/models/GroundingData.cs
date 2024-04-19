using System.Collections.Generic;
using Newtonsoft.Json;
using DistributedChat;

public class GroundingData
{
    [JsonProperty("docs")]
    public List<Doc>? Docs { get; set; }

    [JsonProperty("content")]
    public List<string>? Content { get; set; }

    [JsonProperty("user_query")]
    public string? UserQuery { get; set; }

    [JsonProperty("history")]
    public List<Turn>? History { get; set; }
}