using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.Models.Memory;

public interface IGroundTruthFile
{
    [JsonProperty("ref")]
    [YamlDotNet.Serialization.YamlMember(Alias = "ref")]
    public string? Ref { get; set; }

    [JsonProperty("history")]
    [YamlDotNet.Serialization.YamlMember(Alias = "history")]
    public IList<ITurn>? History { get; set; }

    [JsonProperty("ground_truth")]
    [YamlDotNet.Serialization.YamlMember(Alias = "ground_truth")]
    public string? GroundTruth { get; set; }
}