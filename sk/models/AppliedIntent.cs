using Newtonsoft.Json;

public class AppliedIntent
{
    [JsonProperty("continue", Required = Required.Always)]
    public required bool Continue { get; set; }
}