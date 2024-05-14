using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inference;

public class WorkflowStepResponse<TInput, TOutput>(string name, TInput input, List<LogEntry> logs) : IWorkflowStepResponse
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = name;

    [JsonProperty("input", Required = Required.Always)]
    public TInput Input { get; set; } = input;

    [JsonProperty("output", NullValueHandling = NullValueHandling.Ignore)]
    public TOutput? Output { get; set; }

    [JsonProperty("logs", Required = Required.Always)]
    public List<LogEntry> Logs { get; set; } = logs;
}