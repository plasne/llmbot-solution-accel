using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.Models.Memory;

public interface IInferenceFile
{
    [JsonProperty("ref")]
    public string? Ref { get; set; }

    [JsonProperty("history")]
    public IList<ITurn>? History { get; set; }

    [JsonProperty("ground_truth")]
    public string? GroundTruth { get; set; }

    [JsonProperty("answer")]
    public string? Answer { get; set; }

    [JsonProperty("content")]
    public IList<IContent>? Content { get; set; }
}