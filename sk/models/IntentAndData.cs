using Newtonsoft.Json;

public class IntentAndData
{
    [JsonProperty("intent")]
    public DeterminedIntent? Intent { get; set; }

    [JsonProperty("data")]
    public GroundingData? Data { get; set; }
}