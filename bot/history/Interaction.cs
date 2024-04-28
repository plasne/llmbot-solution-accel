using System;
using DistributedChat;

public enum Roles
{
    UNKNOWN = 0,
    SYSTEM = 1,
    USER = 2,
    ASSISTANT = 3,
}

public enum States
{
    UNKNOWN = 0,
    UNMODIFIED = 1,
    GENERATING = 2,
    STOPPED = 3,
    EDITED = 4,
    DELETED = 5,
    FAILED = 6,
}

public enum Intents
{
    UNKNOWN = 0,
    GREETING = 1,
    GOODBYE = 2,
    IN_DOMAIN = 3,
    OUT_OF_DOMAIN = 4,
    TOPIC_CHANGE = 5,
}

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

    public DateTime Created { get; set; }

    public DateTime Expiry { get; set; }

    public int PromptTokenCount { get; set; }

    public int CompletionTokenCount { get; set; }

    public int TimeToFirstResponse { get; set; }

    public int TimeToLastResponse { get; set; }

    public Turn ToTurn()
    {
        return new Turn { Role = this.Role.ToString().ToLower(), Msg = this.Message };
    }

    public static Interaction CreateUserRequest(string activityId, string userId, string message)
    {
        return new Interaction { ActivityId = activityId, UserId = userId, Role = Roles.USER, Message = message, State = States.UNMODIFIED };
    }

    public static Interaction CreateBotResponse(string activityId, string userId)
    {
        return new Interaction { ActivityId = activityId, UserId = userId, Role = Roles.ASSISTANT, State = States.GENERATING };
    }
}