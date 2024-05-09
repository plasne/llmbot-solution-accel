using System.Collections.Generic;
using Newtonsoft.Json;

public interface IWorkflowResponse
{
    [JsonProperty("answer")]
    public IAnswer? Answer { get; set; }

    [JsonProperty("steps")]
    public IList<IWorkflowStepResponse> Steps { get; set; }
}