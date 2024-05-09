using Newtonsoft.Json;

public interface IIntentAndData
{
    [JsonProperty("intent")]
    public IDeterminedIntent? Intent { get; set; }

    [JsonProperty("data")]
    public IGroundingData? Data { get; set; }
}