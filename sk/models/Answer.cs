using System.Collections.Generic;
using DistributedChat;
using Newtonsoft.Json;

public class Answer
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("citations")]
    public List<Citation>? Citations { get; set; }
}