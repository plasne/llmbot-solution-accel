public class IntentAndData : IIntentAndData
{
    public IDeterminedIntent? Intent { get; set; }
    public IGroundingData? Data { get; set; }
}