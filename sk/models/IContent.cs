using DistributedChat;
using Newtonsoft.Json;

public interface IContent
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("citation")]
    public Citation? Citation { get; set; }
}