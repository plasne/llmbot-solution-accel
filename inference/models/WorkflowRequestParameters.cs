using Newtonsoft.Json;

namespace Inference;

public class WorkflowRequestParameters
{
    [JsonProperty(nameof(INTENT_PROMPT_FILE), NullValueHandling = NullValueHandling.Ignore)]
    public string? INTENT_PROMPT_FILE { get; set; }

    [JsonProperty(nameof(CHAT_PROMPT_FILE), NullValueHandling = NullValueHandling.Ignore)]
    public string? CHAT_PROMPT_FILE { get; set; }

    [JsonProperty(nameof(INTENT_TEMPERATURE), NullValueHandling = NullValueHandling.Ignore)]
    public decimal? INTENT_TEMPERATURE { get; set; }

    [JsonProperty(nameof(CHAT_TEMPERATURE), NullValueHandling = NullValueHandling.Ignore)]
    public decimal? CHAT_TEMPERATURE { get; set; }

    [JsonProperty(nameof(MAX_CONCURRENT_SEARCHES), NullValueHandling = NullValueHandling.Ignore)]
    public int? MAX_CONCURRENT_SEARCHES { get; set; }

    [JsonProperty(nameof(MAX_SEARCH_QUERIES_PER_INTENT), NullValueHandling = NullValueHandling.Ignore)]
    public int? MAX_SEARCH_QUERIES_PER_INTENT { get; set; }

    [JsonProperty(nameof(EXIT_WHEN_OUT_OF_DOMAIN), NullValueHandling = NullValueHandling.Ignore)]
    public bool? EXIT_WHEN_OUT_OF_DOMAIN { get; set; }

    [JsonProperty(nameof(EXIT_WHEN_NO_DOCUMENTS), NullValueHandling = NullValueHandling.Ignore)]
    public bool? EXIT_WHEN_NO_DOCUMENTS { get; set; }

    [JsonProperty(nameof(EXIT_WHEN_NO_CITATIONS), NullValueHandling = NullValueHandling.Ignore)]
    public bool? EXIT_WHEN_NO_CITATIONS { get; set; }
}