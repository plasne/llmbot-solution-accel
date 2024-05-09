using System.Collections.Generic;
using Newtonsoft.Json;

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