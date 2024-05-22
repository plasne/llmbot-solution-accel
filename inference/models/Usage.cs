using Newtonsoft.Json;

namespace Inference;

public class Usage
{
    [JsonProperty("prompt_token_count", Required = Required.Always)]
    public int PromptTokenCount { get; set; }

    [JsonProperty("completion_token_count", Required = Required.Always)]
    public int CompletionTokenCount { get; set; }
}