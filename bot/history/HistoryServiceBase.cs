using System;

public abstract class HistoryServiceBase
{
    public void ValidateAddInteractionAsync(IInteraction interaction)
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

        if (interaction.Role == Roles.Unknown)
        {
            throw new Exception("Role must be specified.");
        }

        if (interaction.State != States.Unmodified && interaction.State != States.Generating)
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
}