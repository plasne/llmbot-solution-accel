using Newtonsoft.Json;

public interface IWorkflowStepResponse
{
    [JsonProperty("name")]
    string Name { get; set; }
}

