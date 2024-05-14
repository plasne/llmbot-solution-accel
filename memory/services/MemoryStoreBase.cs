using System;
using Shared;
using Shared.Models.Memory;

namespace Memory;

public abstract class MemoryStoreBase
{
    public void ValidateInteractionForStartGeneration(Interaction interaction)
    {
        if (interaction.ConversationId != Guid.Empty)
        {
            throw new HttpException(400, "ConversationId will be assigned by the service.");
        }

        if (string.IsNullOrEmpty(interaction.ActivityId))
        {
            throw new HttpException(400, "ActivityId must be provided.");
        }

        if (string.IsNullOrEmpty(interaction.UserId))
        {
            throw new HttpException(400, "UserId must be provided.");
        }

        if (interaction.Role == Roles.UNKNOWN)
        {
            throw new HttpException(400, "Role must be specified.");
        }

        if (interaction.State != States.UNMODIFIED && interaction.State != States.GENERATING)
        {
            throw new HttpException(400, "State must be either UNMODIFIED or GENERATING.");
        }

        if (interaction.Intent != Intents.UNKNOWN)
        {
            throw new HttpException(400, "Intent must not be assigned to a new interaction.");
        }

        if (interaction.Rating is not null || interaction.Comment is not null)
        {
            throw new HttpException(400, "Feedback (Rating/Comment) must not be assigned to a new interaction.");
        }

        if (interaction.PromptTokenCount != 0
            || interaction.CompletionTokenCount != 0
            || interaction.TimeToFirstResponse != 0
            || interaction.TimeToLastResponse != 0)
        {
            throw new HttpException(400, "PromptTokenCount, CompletionTokenCount, TimeToFirstResponse, TimeToLastResponse must not be set in a new interaction.");
        }
    }

    public void ValidateInteractionForCompleteGeneration(Interaction interaction)
    {
        if (interaction.ConversationId == Guid.Empty)
        {
            throw new HttpException(400, "ConversationId must be provided.");
        }

        if (string.IsNullOrEmpty(interaction.ActivityId))
        {
            throw new HttpException(400, "ActivityId must be provided.");
        }

        if (string.IsNullOrEmpty(interaction.UserId))
        {
            throw new HttpException(400, "UserId must be provided.");
        }

        if (interaction.Role == Roles.UNKNOWN)
        {
            throw new HttpException(400, "Role must be specified.");
        }

        if (interaction.State != States.UNMODIFIED
            && interaction.State != States.EMPTY
            && interaction.State != States.STOPPED
            && interaction.State != States.FAILED)
        {
            throw new HttpException(400, "State must be either UNMODIFIED, EMPTY, STOPPED, or FAILED.");
        }

        if (interaction.State == States.EMPTY && !string.IsNullOrEmpty(interaction.Message))
        {
            throw new HttpException(400, "Message must be empty if the state is EMPTY.");
        }

        if (interaction.State != States.EMPTY && interaction.State != States.FAILED && string.IsNullOrEmpty(interaction.Message))
        {
            throw new HttpException(400, "Message must be provided if the state is not EMPTY or FAILED.");
        }

        if (interaction.State != States.FAILED && interaction.Intent == Intents.UNKNOWN)
        {
            throw new HttpException(400, "Intent must be specified unless the state is FAILED.");
        }

        // NOTE: ideally Intent would be set, but in error cases, that isn't always true

        if (interaction.Rating is not null || interaction.Comment is not null)
        {
            throw new HttpException(400, "Feedback (Rating/Comment) must not be assigned to a new interaction.");
        }
    }

    public void ValidateInteractionForTopicChange(Interaction interaction)
    {
        if (interaction.ConversationId == Guid.Empty)
        {
            throw new HttpException(400, "ConversationId must be provided.");
        }

        if (string.IsNullOrEmpty(interaction.ActivityId))
        {
            throw new HttpException(400, "ActivityId must be provided.");
        }

        if (string.IsNullOrEmpty(interaction.UserId))
        {
            throw new HttpException(400, "UserId must be provided.");
        }

        if (interaction.Role == Roles.UNKNOWN)
        {
            throw new HttpException(400, "Role must be specified.");
        }

        if (interaction.State != States.EMPTY)
        {
            throw new HttpException(400, "State must be EMPTY.");
        }

        if (interaction.Intent != Intents.TOPIC_CHANGE)
        {
            throw new HttpException(400, "Intent must be TOPIC_CHANGE.");
        }

        if (interaction.Rating is not null || interaction.Comment is not null)
        {
            throw new HttpException(400, "Feedback (Rating/Comment) must not be assigned to a new interaction.");
        }

        if (interaction.PromptTokenCount != 0
            || interaction.CompletionTokenCount != 0
            || interaction.TimeToFirstResponse != 0
            || interaction.TimeToLastResponse != 0)
        {
            throw new HttpException(400, "PromptTokenCount, CompletionTokenCount, TimeToFirstResponse, TimeToLastResponse must not be set in a new interaction.");
        }
    }
}