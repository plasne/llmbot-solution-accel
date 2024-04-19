using Newtonsoft.Json;

public class IntentAndData
{
    [JsonProperty("intent")]
    public Intent? Intent { get; set; }

    [JsonProperty("data")]
    public GroundingData? Data { get; set; }
}