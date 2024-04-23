using System.Collections.Generic;
using Newtonsoft.Json;

public class WorkflowResponse()
{
    [JsonProperty("answer")]
    public Answer? Answer { get; set; }

    [JsonProperty("steps")]
    public List<IWorkflowStepResponse> Steps { get; set; } = [];
}

public interface IWorkflowStepResponse
{
    [JsonProperty("name")]
    string Name { get; set; }
}

public class WorkflowStepResponse<TInput, TOutput>(string name, TInput input, List<LogEntry> logs) : IWorkflowStepResponse
{
    [JsonProperty("name")]
    public string Name { get; set; } = name;

    [JsonProperty("input")]
    public TInput Input { get; set; } = input;

    [JsonProperty("output")]
    public TOutput? Output { get; set; }

    [JsonProperty("logs")]
    public List<LogEntry> Logs { get; set; } = logs;
}