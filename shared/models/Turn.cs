using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.Models.Memory;

public class Turn
{
    [JsonProperty("role", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public required Roles Role { get; set; }

    [JsonProperty("msg", Required = Required.Always)]
    public required string Msg { get; set; }
}