using System;
using Shared.Models.Memory;

public class CompleteGenerationRequest(string activityId) : ICompleteGenerationRequest
{
    public Guid ConversationId { get; set; }
    public string ActivityId { get; set; } = activityId;
    public string Message { get; set; } = "";
    public Intents Intent { get; set; }
    public States State { get; set; }
    public int PromptTokenCount { get; set; }
    public int CompletionTokenCount { get; set; }
    public int TimeToFirstResponse { get; set; }
    public int TimeToLastResponse { get; set; }
}