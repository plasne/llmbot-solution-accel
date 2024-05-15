using Newtonsoft.Json;

namespace Inference;

public class Citation
{
    [JsonProperty("id", Required = Required.Always)]
    public required string Id { get; set; }

    [JsonProperty("title", Required = Required.Always)]
    public required string Title { get; set; }

    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string? Uri { get; set; }
}