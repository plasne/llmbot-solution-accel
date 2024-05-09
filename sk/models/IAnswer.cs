using System.Collections.Generic;
using DistributedChat;
using Newtonsoft.Json;

public interface IAnswer
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("citations")]
    public List<Citation>? Citations { get; set; }
}