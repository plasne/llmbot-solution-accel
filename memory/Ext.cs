using System;
using Iso8601DurationHelper;
using Shared.Models.Memory;

public static class Ext
{
    public static Duration AsDuration(this string value, Func<Duration> dflt)
    {
        if (Duration.TryParse(value, out var duration))
        {
            return duration;
        }
        return dflt();
    }

    public static (Interaction req, Interaction res) ToInteractions(this IStartGenerationRequest req, string userId)
    {
        return (
            new Interaction
            {
                ActivityId = req.RequestActivityId,
                UserId = userId,
                Role = Roles.USER,
                Message = req.Query,
                State = States.UNMODIFIED
            },
            new Interaction
            {
                ActivityId = req.ResponseActivityId,
                UserId = userId,
                Role = Roles.ASSISTANT,
                State = States.GENERATING
            }
        );
    }

    public static Interaction ToInteraction(this IChangeTopicRequest req, string userId)
    {
        return new Interaction
        {
            ConversationId = Guid.NewGuid(),
            ActivityId = req.ActivityId,
            UserId = userId,
            Role = Roles.USER,
            Intent = Intents.TOPIC_CHANGE,
            State = States.EMPTY
        };
    }

    public static Interaction ToInteraction(this ICompleteGenerationRequest res, string userId)
    {
        return new Interaction
        {
            ConversationId = res.ConversationId,
            ActivityId = res.ActivityId,
            UserId = userId,
            Role = Roles.SYSTEM,
            Message = res.Message,
            Intent = res.Intent,
            State = res.State,
            PromptTokenCount = res.PromptTokenCount,
            CompletionTokenCount = res.CompletionTokenCount,
            TimeToFirstResponse = res.TimeToFirstResponse,
            TimeToLastResponse = res.TimeToLastResponse
        };
    }
}