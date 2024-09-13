using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iso8601DurationHelper;
using Shared.Models;
using Shared;
using Shared.Models.Memory;

namespace Memory;

public class LocalMemoryStore() : MemoryStoreBase, IMemoryStore
{
    private readonly Dictionary<string, List<Interaction>> interactions = [];

    public Task<Guid> StartGenerationAsync(Interaction request, Interaction response, Duration expiry, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForStartGeneration(request);
        base.ValidateInteractionForStartGeneration(response);

        if (!this.interactions.TryGetValue(request.UserId!, out List<Interaction>? list))
        {
            var conversationId = Guid.NewGuid();
            request.ConversationId = conversationId;
            response.ConversationId = conversationId;
            this.interactions.Add(request.UserId!, [request, response]);
            return Task.FromResult(request.ConversationId);
        }

        var last = list[^1];
        if (last.State == States.GENERATING)
        {
            throw new HttpException(423, "a response is already being generated.");
        }

        request.ConversationId = last.ConversationId;
        response.ConversationId = last.ConversationId;
        list.Add(request);
        list.Add(response);

        return Task.FromResult(request.ConversationId);
    }

    public Task CompleteGenerationAsync(Interaction response, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForCompleteGeneration(response);
        // already mutated
        return Task.CompletedTask;
    }

    public Task ChangeConversationTopicAsync(Interaction changeTopic, Duration expiry, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForTopicChange(changeTopic);

        if (!this.interactions.TryGetValue(changeTopic.UserId!, out List<Interaction>? list))
        {
            this.interactions.Add(changeTopic.UserId!, [changeTopic]);
            return Task.CompletedTask;
        }

        list.Add(changeTopic);
        return Task.CompletedTask;
    }

    public Task ClearLastFeedbackAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task ClearFeedbackAsync(string userId, string activityId, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task CommentOnMessageAsync(string userId, string activityId, string comment, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task CommentOnLastMessageAsync(string userId, string comment, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task<IEnumerable<DeletedUserMessage>> DeleteActivitiesAsync(string userId, int count = 1, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }
    public Task DeleteActivityAsync(string userId, string activityId, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task<Conversation> GetLastConversationAsync(string userId, int? maxTokens, string? modelName, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation { Id = Guid.Empty, Turns = [] };

        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            return Task.FromResult(conversation);
        }

        var last = list[^1];
        var filtered = list
            .Where(x => x.ConversationId == last.ConversationId)
            .Where(x => x.State == States.EDITED || x.State == States.STOPPED || x.State == States.UNMODIFIED);

        int totalTokenCount = 0;
        foreach (var interaction in filtered)
        {
            conversation.Id = interaction.ConversationId;

            if (maxTokens is not null && modelName is not null && interaction.Message is not null &&
                IsMaxTokenLimitExceeded(modelName, maxTokens.Value, interaction.Message, ref totalTokenCount))
            {
                break;
            }

            conversation.Turns.Add(new Turn { Role = interaction.Role, Msg = interaction.Message! });
        }

        return Task.FromResult(conversation);
    }

    public Task RateLastMessageAsync(string userId, string rating, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task RateMessageAsync(string userId, string activityId, string rating, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task<Interaction> GetInteractionAsync(string userId, string? activityId, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task SetCustomInstructionsAsync(string userId, CustomInstructions instructions, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task DeleteCustomInstructionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task<CustomInstructions> GetCustomInstructionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task UpdateUserMessage(Interaction response, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}