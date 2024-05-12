using System.Collections.Generic;
using Newtonsoft.Json;

public class WorkflowResponse
{
    [JsonProperty("answer", NullValueHandling = NullValueHandling.Ignore)]
    public Answer? Answer { get; set; }

    [JsonProperty("steps", Required = Required.Always)]
    public IList<IWorkflowStepResponse> Steps { get; set; } = [];
}