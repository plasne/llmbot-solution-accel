using DistributedChat;
using Newtonsoft.Json;

public class Content
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("citation")]
    public Citation? Citation { get; set; }
}