using System.Collections.Generic;
using DistributedChat;
using Newtonsoft.Json;

public class InferenceRequestFile
{
    [JsonProperty("ref")]
    public string? Ref { get; set; }

    [JsonProperty("history")]
    public List<Turn>? History { get; set; }

    [JsonProperty("ground_truth")]
    public string? GroundTruth { get; set; }
}