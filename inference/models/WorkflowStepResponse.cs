using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Inference;

public class WorkflowStepResponse<TInput, TOutput>(List<LogEntry> logs, Usage usage) : IWorkflowStepResponse
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("input", NullValueHandling = NullValueHandling.Ignore)]
    public TInput? Input { get; set; }

    [JsonProperty("output", NullValueHandling = NullValueHandling.Ignore)]
    public TOutput? Output { get; set; }

    [JsonProperty("logs", Required = Required.Always)]
    public List<LogEntry> Logs { get; set; } = logs;

    [JsonProperty("usage", Required = Required.Always)]
    public Usage Usage { get; set; } = usage;

    [JsonProperty("continue", Required = Required.Always)]
    public bool Continue { get; set; } = true;
}