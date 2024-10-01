using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.Models.Memory;

namespace Inference;

public class WorkflowRequest
{
    [JsonProperty("user_query")]
    public required string UserQuery { get; set; }

    [JsonProperty("previous_topic_change", NullValueHandling = NullValueHandling.Ignore)]
    public bool? PreviousTopicChange { get; set; }

    [JsonProperty("history", NullValueHandling = NullValueHandling.Ignore)]
    public List<Turn>? History { get; set; }

    [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Context { get; set; }

    [JsonProperty("custom_instructions", NullValueHandling = NullValueHandling.Ignore)]
    public string? CustomInstructions { get; set; }
}