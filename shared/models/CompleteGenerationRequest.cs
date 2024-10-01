using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.Models.Memory;

public class CompleteGenerationRequest
{
    [JsonProperty("conversation_id", Required = Required.Always)]
    public required Guid ConversationId { get; set; }

    [JsonProperty("activity_id", Required = Required.Always)]
    public required string ActivityId { get; set; }

    [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
    public string? Message { get; set; }

    [JsonProperty("citations", NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<Citation>? Citations { get; set; }

    [JsonProperty("intent", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public required Intents Intent { get; set; }

    [JsonProperty("state", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public required States State { get; set; }

    [JsonProperty("prompt_token_count")]
    public int PromptTokenCount { get; set; }

    [JsonProperty("completion_token_count")]
    public int CompletionTokenCount { get; set; }

    [JsonProperty("embedding_token_count")]
    public int EmbeddingTokenCount { get; set; }

    [JsonProperty("time_to_first_response")]
    public int TimeToFirstResponse { get; set; }

    [JsonProperty("time_to_last_response")]
    public int TimeToLastResponse { get; set; }
}