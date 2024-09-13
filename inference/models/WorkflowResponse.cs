using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inference;

public class WorkflowResponse
{
    [JsonProperty("answer", NullValueHandling = NullValueHandling.Ignore)]
    public Answer? Answer { get; set; }

    [JsonProperty("config", Required = Required.Always)]
    public required IConfig Config { get; set; }

    [JsonProperty("steps", Required = Required.Always)]
    public List<IWorkflowStepResponse> Steps { get; set; } = [];
}