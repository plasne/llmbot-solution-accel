using Newtonsoft.Json;

namespace Inference;

public class WorkflowRequestParameters
{
    [JsonProperty(nameof(INTENT_PROMPT_FILE), NullValueHandling = NullValueHandling.Ignore)]
    public string? INTENT_PROMPT_FILE { get; set; }

    [JsonProperty(nameof(CHAT_PROMPT_FILE), NullValueHandling = NullValueHandling.Ignore)]
    public string? CHAT_PROMPT_FILE { get; set; }
}