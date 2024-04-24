using Newtonsoft.Json;

public class PipelineRequest
{
    [JsonProperty("ground_truth_uri")]
    public string? GroundTruthUri { get; set; }

    [JsonProperty("inference_uri")]
    public string? InferenceUri { get; set; }

    [JsonProperty("evaluation_uri")]
    public string? EvaluationUri { get; set; }

    [JsonProperty("project")]
    public string? Project { get; set; }

    [JsonProperty("experiment")]
    public string? Experiment { get; set; }

    [JsonProperty("ref")]
    public string? Ref { get; set; }

    [JsonProperty("set")]
    public string? Set { get; set; }

    [JsonProperty("is_baseline")]
    public bool IsBaseline { get; set; }
}