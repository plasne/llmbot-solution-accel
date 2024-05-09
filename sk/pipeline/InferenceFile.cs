using System.Collections.Generic;
using Newtonsoft.Json;

public class InferenceFile
{
    [JsonProperty("ref", Required = Required.Always)]
    public required string Ref { get; set; }

    [JsonProperty("history", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public IList<Turn>? History { get; set; }

    [JsonProperty("ground_truth", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? GroundTruth { get; set; }

    [JsonProperty("answer", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? Answer { get; set; }

    [JsonProperty("content", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public IList<Content>? Content { get; set; }
}