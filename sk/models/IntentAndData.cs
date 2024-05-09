using Newtonsoft.Json;

public class IntentAndData
{
    [JsonProperty("intent", Required = Required.Always)]
    public required DeterminedIntent Intent { get; set; }

    [JsonProperty("data", Required = Required.Always)]
    public required GroundingData Data { get; set; }
}