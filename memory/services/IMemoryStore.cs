using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Iso8601DurationHelper;
using Shared.Models;
using Shared.Models.Memory;

namespace Memory;

public interface IMemoryStore
{
    Task<Conversation> GetLastConversationAsync(string userId, int? maxTokens, string? modelName, CancellationToken cancellationToken = default);

    Task RateLastMessageAsync(string userId, string rating, CancellationToken cancellationToken = default);

    Task<Interaction> GetLastInteractionAsync(string userId, CancellationToken cancellationToken = default);

    Task<Interaction> GetInteractionAsync(string userId, string activityId, CancellationToken cancellationToken = default);

    Task<Guid> StartGenerationAsync(Interaction request, Interaction response, Duration expiry, CancellationToken cancellationToken = default);

    Task CompleteGenerationAsync(Interaction response, CancellationToken cancellationToken = default);

    Task UpdateUserMessageAsync(Interaction response, CancellationToken cancellationToken = default);

    Task<IEnumerable<DeletedUserMessage>> DeleteActivitiesAsync(string userId, int count = 1, CancellationToken cancellationToken = default);

    Task CommentOnLastMessageAsync(string userId, string comment, CancellationToken cancellationToken = default);

    Task DeleteActivityAsync(string userId, string activityId, CancellationToken cancellationToken = default);

    Task ChangeConversationTopicAsync(Interaction changeTopic, Duration expiry, CancellationToken cancellationToken = default);

    Task RateMessageAsync(string userId, string activityId, string rating, CancellationToken cancellationToken = default);

    Task CommentOnMessageAsync(string userId, string activityId, string comment, CancellationToken cancellationToken = default);

    Task ClearLastFeedbackAsync(string userId, CancellationToken cancellationToken = default);

    Task ClearFeedbackAsync(string userId, string activityId, CancellationToken cancellationToken = default);

    Task SetCustomInstructionsAsync(string userId, CustomInstructions instructions, CancellationToken cancellationToken = default);

    Task DeleteCustomInstructionsAsync(string userId, CancellationToken cancellationToken = default);

    Task<CustomInstructions> GetCustomInstructionsAsync(string userId, CancellationToken cancellationToken = default);
}