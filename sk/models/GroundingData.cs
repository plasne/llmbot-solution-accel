using System.Collections.Generic;
using DistributedChat;
using Newtonsoft.Json;

public class GroundingData
{
    [JsonProperty("docs")]
    public List<Doc>? Docs { get; set; }

    [JsonProperty("content")]
    public List<Content>? Content { get; set; }

    [JsonProperty("user_query")]
    public string? UserQuery { get; set; }

    [JsonProperty("history")]
    public List<Turn>? History { get; set; }
}