using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inference;

public class Answer
{
    [JsonProperty("text", Required = Required.Always)]
    public required string Text { get; set; }

    [JsonProperty("citations", NullValueHandling = NullValueHandling.Ignore)]
    public List<Citation>? Citations { get; set; }
}