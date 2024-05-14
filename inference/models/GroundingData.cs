using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inference;

public class GroundingData
{
    [JsonProperty("docs", NullValueHandling = NullValueHandling.Ignore)]
    public List<Doc>? Docs { get; set; }

    [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
    public List<Context>? Context { get; set; }

    [JsonProperty("user_query")]
    public required string UserQuery { get; set; }

    [JsonProperty("history", NullValueHandling = NullValueHandling.Ignore)]
    public List<Turn>? History { get; set; }
}