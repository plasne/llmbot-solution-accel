using System.Threading.Tasks;
using Iso8601DurationHelper;

public interface IHistoryService
{
    Task<Conversation> GetCurrentConversationAsync(string userId);

    Task StartGenerationAsync(Interaction request, Interaction response, Duration expiry);

    Task CompleteGenerationAsync(Interaction response);

    Task DeleteLastInteractionsAsync(string userId, int count = 1);

    Task<Conversation> ChangeConversationTopicAsync(string userId);

    Task RateMessageAsync(string userId, string rating);

    Task RateMessageAsync(string userId, string activityId, string rating);

    Task CommentOnMessageAsync(string userId, string comment);

    Task CommentOnMessageAsync(string userId, string activityId, string comment);

    Task ClearFeedbackAsync(string userId);

    Task ClearFeedbackAsync(string userId, string activityId);

    Task StartupAsync();
}