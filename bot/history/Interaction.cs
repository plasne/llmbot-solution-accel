using System;
using DistributedChat;

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

public class Interaction
{
    public Guid? ConversationId { get; set; }

    public string? ActivityId { get; set; }

    public string? UserId { get; set; }

    public Roles Role { get; set; }

    public string? Message { get; set; }

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
        return new Interaction { ActivityId = activityId, UserId = userId, Role = Roles.User, Message = message, State = States.Unmodified };
    }

    public static Interaction CreateBotResponse(string activityId, string userId)
    {
        return new Interaction { ActivityId = activityId, UserId = userId, Role = Roles.Assistant, State = States.Generating };
    }
}