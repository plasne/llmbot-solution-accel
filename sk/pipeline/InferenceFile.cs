using System.Collections.Generic;
using Newtonsoft.Json;

public class InferenceFile
{
    [JsonProperty("ref", Required = Required.Always)]
    public required string Ref { get; set; }

    [JsonProperty("history", NullValueHandling = NullValueHandling.Ignore)]
    public IList<Turn>? History { get; set; }

    [JsonProperty("ground_truth", NullValueHandling = NullValueHandling.Ignore)]
    public string? GroundTruth { get; set; }

    [JsonProperty("answer", NullValueHandling = NullValueHandling.Ignore)]
    public string? Answer { get; set; }

    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    public IList<Context>? Context { get; set; }
}