using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class UserMessageRequest
{
  [JsonProperty("activity_id", Required = Required.Always)]
  public required string ActivityId { get; set; }

  [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
  public string? Message { get; set; }
}