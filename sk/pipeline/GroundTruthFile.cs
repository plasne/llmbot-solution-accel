using System.Collections.Generic;
using Newtonsoft.Json;

public class PipelineTurn
{
    [JsonProperty("role")]
    [YamlDotNet.Serialization.YamlMember(Alias = "role")]
    public string? Role { get; set; }

    [JsonProperty("msg")]
    [YamlDotNet.Serialization.YamlMember(Alias = "msg")]
    public string? Msg { get; set; }
}

public class GroundTruthFile
{
    [JsonProperty("ref")]
    [YamlDotNet.Serialization.YamlMember(Alias = "ref")]
    public string? Ref { get; set; }

    [JsonProperty("history")]
    [YamlDotNet.Serialization.YamlMember(Alias = "history")]
    public List<PipelineTurn>? History { get; set; }

    [JsonProperty("ground_truth")]
    [YamlDotNet.Serialization.YamlMember(Alias = "ground_truth")]
    public string? GroundTruth { get; set; }
}