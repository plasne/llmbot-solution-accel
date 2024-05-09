using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.Models.Memory;

public interface IGroundingData
{
    [JsonProperty("docs")]
    public IList<IDoc>? Docs { get; set; }

    [JsonProperty("content")]
    public IList<IContent>? Content { get; set; }

    [JsonProperty("user_query")]
    public string UserQuery { get; set; }

    [JsonProperty("history")]
    public IList<ITurn>? History { get; set; }
}