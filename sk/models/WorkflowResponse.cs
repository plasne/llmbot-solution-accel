using System.Collections.Generic;
using Newtonsoft.Json;

public class WorkflowResponse
{
    [JsonProperty("answer", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public Answer? Answer { get; set; }

    [JsonProperty("steps", Required = Required.Always)]
    public IList<IWorkflowStepResponse> Steps { get; set; } = [];
}