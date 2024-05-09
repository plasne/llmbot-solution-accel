using System.Collections.Generic;
using DistributedChat;
using Newtonsoft.Json;

public class Answer
{
    [JsonProperty("text", Required = Required.Always)]
    public required string Text { get; set; }

    [JsonProperty("citations", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public IList<Citation>? Citations { get; set; }
}