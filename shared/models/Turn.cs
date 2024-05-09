using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Models.Memory;

public class Turn
{
    [JsonProperty("role", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public Roles Role { get; set; }

    [JsonProperty("msg", Required = Required.AllowNull)]
    public string? Msg { get; set; }
}