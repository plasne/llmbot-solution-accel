using System;
using Shared.Models.Memory;

public class Interaction
{
    public Guid? ConversationId { get; set; }

    public string? ActivityId { get; set; }

    public string? UserId { get; set; }

    public Roles Role { get; set; }

    public string? Message { get; set; }

    public Intents Intent { get; set; }

    public States State { get; set; }

    public string? Rating { get; set; }

    public string? Comment { get; set; }

    public int PromptTokenCount { get; set; }

    public int CompletionTokenCount { get; set; }

    public int TimeToFirstResponse { get; set; }

    public int TimeToLastResponse { get; set; }
}