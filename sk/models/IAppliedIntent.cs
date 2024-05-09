using Newtonsoft.Json;

public interface IAppliedIntent
{
    [JsonProperty("continue")]
    public bool Continue { get; set; }
}