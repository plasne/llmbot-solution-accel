using System;

public abstract class HistoryServiceBase
{
    public void ValidateInteractionForStartGeneration(Interaction interaction)
    {
        if (interaction.ConversationId is not null)
        {
            throw new Exception("ConversationId will be assigned by the service.");
        }

        if (string.IsNullOrEmpty(interaction.ActivityId))
        {
            throw new Exception("ActivityId must be provided.");
        }

        if (string.IsNullOrEmpty(interaction.UserId))
        {
            throw new Exception("UserId must be provided.");
        }

        if (interaction.Role == Roles.UNKNOWN)
        {
            throw new Exception("Role must be specified.");
        }

        if (interaction.State != States.UNMODIFIED && interaction.State != States.GENERATING)
        {
            throw new Exception("State must be either Unmodified or Generating.");
        }

        if (interaction.Rating is not null || interaction.Comment is not null)
        {
            throw new Exception("Feedback (Rating/Comment) must not be assigned to a new interaction.");
        }

        if (interaction.PromptTokenCount != 0
            || interaction.CompletionTokenCount != 0
            || interaction.TimeToFirstResponse != 0
            || interaction.TimeToLastResponse != 0)
        {
            throw new Exception("PromptTokenCount, CompletionTokenCount, TimeToFirstResponse, TimeToLastResponse must not be set in a new interaction.");
        }
    }

    public void ValidateInteractionForCompleteGeneration(Interaction interaction)
    {
        if (string.IsNullOrEmpty(interaction.ActivityId))
        {
            throw new Exception("ActivityId must be provided.");
        }

        if (string.IsNullOrEmpty(interaction.UserId))
        {
            throw new Exception("UserId must be provided.");
        }

        if (interaction.State != States.UNMODIFIED && interaction.State != States.STOPPED && interaction.State != States.FAILED)
        {
            throw new Exception("State must be either Unmodified, Stopped, or Failed.");
        }

        if (interaction.Rating is not null || interaction.Comment is not null)
        {
            throw new Exception("Feedback (Rating/Comment) must not be assigned to a new interaction.");
        }
    }
}