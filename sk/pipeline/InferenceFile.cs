using System.Collections.Generic;
using Newtonsoft.Json;

public class InferenceFile
{
    [JsonProperty("ref")]
    public string? Ref { get; set; }

    [JsonProperty("history")]
    public List<PipelineTurn>? History { get; set; }

    [JsonProperty("ground_truth")]
    public string? GroundTruth { get; set; }

    [JsonProperty("answer")]
    public string? Answer { get; set; }

    [JsonProperty("content")]
    public List<Content>? Content { get; set; }
}