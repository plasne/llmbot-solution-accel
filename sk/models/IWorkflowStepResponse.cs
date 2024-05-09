using Newtonsoft.Json;

public interface IWorkflowStepResponse
{
    [JsonProperty("name", Required = Required.Always)]
    string Name { get; set; }
}

