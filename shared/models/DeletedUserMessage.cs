using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Models.Memory;

namespace Shared.Models.Memory;

public class DeletedUserMessage
{
    [JsonProperty("activity_id", Required = Required.Always)]
    public required string ActivityId { get; set; }

    [JsonProperty("role", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public required Roles Role { get; set; }
}