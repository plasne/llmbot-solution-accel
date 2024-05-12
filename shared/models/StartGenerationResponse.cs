using Newtonsoft.Json;

namespace Shared.Models.Memory;

public class StartGenerationResponse
{
    [JsonProperty("conversation_id", Required = Required.Always)]
    public required Guid ConversationId { get; set; }
}