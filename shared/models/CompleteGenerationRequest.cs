using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.Models.Memory;

public class CompleteGenerationRequest
{
    [JsonProperty("conversation_id", Required = Required.Always)]
    public required Guid ConversationId { get; set; }

    [JsonProperty("activity_id", Required = Required.Always)]
    public required string ActivityId { get; set; }

    [JsonProperty("message", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? Message { get; set; }

    [JsonProperty("intent", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public required Intents Intent { get; set; }

    [JsonProperty("state", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public required States State { get; set; }

    [JsonProperty("prompt_token_count", Required = Required.Default)]
    public int PromptTokenCount { get; set; }

    [JsonProperty("completion_token_count", Required = Required.Default)]
    public int CompletionTokenCount { get; set; }

    [JsonProperty("time_to_first_response", Required = Required.Default)]
    public int TimeToFirstResponse { get; set; }

    [JsonProperty("time_to_last_response", Required = Required.Default)]
    public int TimeToLastResponse { get; set; }
}