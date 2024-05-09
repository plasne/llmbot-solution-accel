using Newtonsoft.Json;

public class Conversation
{
    [JsonProperty("id", Required = Required.Always)]
    public required Guid Id { get; set; }

    [JsonProperty("turns", Required = Required.Always)]
    public required IList<Turn> Turns { get; set; }

    [JsonProperty("custom_instructions", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string? CustomInstructions { get; set; }
}