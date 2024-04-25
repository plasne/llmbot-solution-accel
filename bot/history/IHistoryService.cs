using System.Threading.Tasks;

public interface IHistoryService
{
    Task<IConversation> GetCurrentConversationAsync(string userId);

    Task AddInteractionAsync(IInteraction interaction);

    Task DeleteLastInteractionsAsync(string userId, int count = 1);

    Task<IConversation> ChangeConversationTopicAsync(string userId);

    Task RateMessageAsync(string userId, string rating);

    Task RateMessageAsync(string userId, string activityId, string rating);

    Task CommentOnMessageAsync(string userId, string comment);

    Task CommentOnMessageAsync(string userId, string activityId, string comment);

    Task ClearFeedbackAsync(string userId);

    Task ClearFeedbackAsync(string userId, string activityId);
}