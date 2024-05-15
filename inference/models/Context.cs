using Newtonsoft.Json;

namespace Inference;

public class Context
{
    [JsonProperty("text", Required = Required.Always)]
    public required string Text { get; set; }

    [JsonProperty("citation", Required = Required.Always)]
    public required Citation Citation { get; set; }
}