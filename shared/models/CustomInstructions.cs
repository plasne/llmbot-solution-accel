using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class CustomInstructions
{
    [JsonProperty("prompt", Required = Required.Always)]
    public required string Prompt { get; set; }
}