using System;

public enum Roles
{
    Unknown = 0,
    System = 1,
    User = 2,
    Assistant = 3,
}

public enum States
{
    Unknown = 0,
    Unmodified = 1,
    Generating = 2,
    Stopped = 3,
    Edited = 4,
    Deleted = 5,
}

public interface IInteraction
{
    string ConversationId { get; set; }

    string ActivityId { get; set; }

    string UserId { get; set; }

    Roles Role { get; set; }

    string Message { get; set; }

    States State { get; set; }

    string Rating { get; set; }

    string Comment { get; set; }

    DateTime Created { get; set; }

    DateTime Expiry { get; set; }

    int PromptTokenCount { get; set; }

    int CompletionTokenCount { get; set; }

    int TimeToFirstResponse { get; set; }

    int TimeToLastResponse { get; set; }
}