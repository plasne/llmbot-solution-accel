using DistributedChat;
using Newtonsoft.Json;

public class Content
{
    [JsonProperty("text", Required = Required.Always)]
    public required string Text { get; set; }

    [JsonProperty("citations", Required = Required.Always)]
    public required Citation Citation { get; set; }
}