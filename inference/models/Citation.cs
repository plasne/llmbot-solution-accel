using Newtonsoft.Json;

namespace Inference;

public class Context
{
    [JsonProperty("id", Required = Required.Always)]
    public required string Id { get; set; }

    [JsonProperty("title", Required = Required.Always)]
    public required string Title { get; set; }

    [JsonProperty("uris", NullValueHandling = NullValueHandling.Ignore)]
    public string[]? Uris { get; set; }

    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }
}