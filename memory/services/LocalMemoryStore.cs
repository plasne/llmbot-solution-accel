using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iso8601DurationHelper;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Models.Memory;

public class LocalMemoryStore(ILogger<LocalMemoryStore> logger)
: MemoryStoreBase, IMemoryStore
{
    private readonly ILogger<LocalMemoryStore> logger = logger;
    private readonly Dictionary<string, List<Interaction>> interactions = [];

    public Task StartGenerationAsync(Interaction request, Interaction response, Duration expiry, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForStartGeneration(request);
        base.ValidateInteractionForStartGeneration(response);

        if (!this.interactions.TryGetValue(request.UserId!, out List<Interaction>? interactions))
        {
            var conversationId = Guid.NewGuid();
            request.ConversationId = conversationId;
            response.ConversationId = conversationId;
            this.interactions.Add(request.UserId!, [request, response]);
            return Task.CompletedTask;
        }

        var last = interactions!.Last();
        if (last.State == States.GENERATING)
        {
            throw new HttpException(423, "a response is already being generated.");
        }

        request.ConversationId = last.ConversationId;
        response.ConversationId = last.ConversationId;
        interactions.Add(request);
        interactions.Add(response);

        return Task.CompletedTask;
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

        if (!this.interactions.TryGetValue(changeTopic.UserId!, out List<Interaction>? interactions))
        {
            this.interactions.Add(changeTopic.UserId!, [changeTopic]);
            return Task.CompletedTask;
        }

        interactions.Add(changeTopic);
        return Task.CompletedTask;
    }

    public Task ClearFeedbackAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task ClearFeedbackAsync(string userId, string activityId, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task CommentOnMessageAsync(string userId, string comment, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task CommentOnMessageAsync(string userId, string activityId, string comment, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task DeleteLastInteractionsAsync(string userId, int count = 1, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task<Conversation> GetCurrentConversationAsync(string userId, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation { Id = Guid.Empty, Turns = [] };

        if (!this.interactions.TryGetValue(userId, out List<Interaction>? interactions))
        {
            return Task.FromResult(conversation);
        }

        var last = interactions.Last();
        var filtered = interactions
            .Where(x => x.ConversationId == last.ConversationId)
            .Where(x => x.State == States.EDITED || x.State == States.STOPPED || x.State == States.UNMODIFIED);
        foreach (var interaction in filtered)
        {
            conversation.Id = interaction.ConversationId;
            conversation.Turns.Add(new Turn { Role = interaction.Role, Msg = interaction.Message! });
        }

        return Task.FromResult(conversation);
    }

    public Task RateMessageAsync(string userId, string rating, CancellationToken cancellationToken = default)
    {
        throw new HttpException(501, "not currently implemented");
    }

    public Task RateMessageAsync(string userId, string activityId, string rating, CancellationToken cancellationToken = default)
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
}