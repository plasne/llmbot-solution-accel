using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inference;

public class WorkflowResponse
{
    [JsonProperty("answer", NullValueHandling = NullValueHandling.Ignore)]
    public Answer? Answer { get; set; }

    [JsonProperty("steps", Required = Required.Always)]
    public List<IWorkflowStepResponse> Steps { get; set; } = [];
}