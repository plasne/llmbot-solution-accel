using System.Collections.Generic;
using Newtonsoft.Json;

public class GroundTruthFile
{
    [JsonProperty("ref", Required = Required.Always)]
    [YamlDotNet.Serialization.YamlMember(Alias = "ref")]
    public required string Ref { get; set; }

    [JsonProperty("history", Required = Required.Always)]
    [YamlDotNet.Serialization.YamlMember(Alias = "history")]
    public required IList<Turn> History { get; set; }

    [JsonProperty("ground_truth", Required = Required.Always)]
    [YamlDotNet.Serialization.YamlMember(Alias = "ground_truth")]
    public required string GroundTruth { get; set; }
}