using System;
using System.Text;
using Iso8601DurationHelper;
using Newtonsoft.Json;
using Shared.Models;
using Shared;
using Shared.Models.Memory;

namespace Memory;

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

    public static bool TryDecodeBase64String(this string value, out string decoded)
    {
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            return true;
        }
        catch
        {
            decoded = string.Empty;
            return false;
        }
    }

    public static string Decode(this string activityId)
    {
        if (long.TryParse(activityId, out var asLong))
        {
            return asLong.ToString();
        }
        else if (activityId.TryDecodeBase64String(out var asDecoded))
        {
            return asDecoded;
        }
        else
        {
            throw new HttpException(400, "activityId must be an int or base64-encoded string.");
        }
    }

    public static (Interaction req, Interaction res) ToInteractions(this StartGenerationRequest req, string userId)
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

    public static Interaction ToInteraction(this ChangeTopicRequest req, string userId)
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

    public static Interaction ToInteraction(this CompleteGenerationRequest res, string userId)
    {
        return new Interaction
        {
            ConversationId = res.ConversationId,
            ActivityId = res.ActivityId,
            UserId = userId,
            Role = Roles.SYSTEM,
            Message = res.Message,
            Citations = JsonConvert.SerializeObject(res.Citations),
            Intent = res.Intent,
            State = res.State,
            PromptTokenCount = res.PromptTokenCount,
            CompletionTokenCount = res.CompletionTokenCount,
            EmbeddingTokenCount = res.EmbeddingTokenCount,
            TimeToFirstResponse = res.TimeToFirstResponse,
            TimeToLastResponse = res.TimeToLastResponse
        };
    }

    public static Interaction ToInteraction(this UserMessageRequest res, string userId)
    {
        return new Interaction
        {
            ActivityId = res.ActivityId,
            UserId = userId,
            Role = Roles.USER,
            Message = res.Message,
        };
    }

    public static FeedbackRequest ToFeedBackRequest(this Interaction res)
    {
        return new FeedbackRequest
        {
            Rating = string.IsNullOrEmpty(res.Rating) ? null : res.Rating,
            Comment = string.IsNullOrEmpty(res.Comment) ? null : res.Comment,
            Message = string.IsNullOrEmpty(res.Message) ? null : res.Message,
            Citations = string.IsNullOrEmpty(res.Citations) ? null : res.Citations,
        };
    }
}