using Newtonsoft.Json;

public class WorkflowResponse(string answer)
{
    [JsonProperty("answer")]
    public string Answer { get; set; } = answer;
}