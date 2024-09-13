using Newtonsoft.Json;
using Shared.Models.Memory;

namespace Shared.Models.Memory;

public class DeletedUserMessage
{
    [JsonProperty("activity_id", Required = Required.Always)]
    public required string ActivityId { get; set; }

    [JsonProperty("role", Required = Required.Always)]
    public required Roles Role { get; set; }
}