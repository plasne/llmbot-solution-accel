using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class Citation
{
    [JsonProperty("ref", Required = Required.Always)]
    public required string Ref  { get; set; }

    [JsonProperty("uri", Required = Required.Always)]
    public required string Uri  { get; set; }
}