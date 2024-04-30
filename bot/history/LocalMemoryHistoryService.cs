using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Iso8601DurationHelper;
using Microsoft.Extensions.Logging;

public class LocalMemoryHistoryService(ILogger<LocalMemoryHistoryService> logger)
: HistoryServiceBase, IHistoryService
{
    private readonly ILogger<LocalMemoryHistoryService> logger = logger;
    private readonly Dictionary<string, List<Interaction>> interactions = new();

    public Task StartGenerationAsync(Interaction request, Interaction response, Duration expiry)
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
            throw new AlreadyGeneratingException(request.UserId!);
        }

        request.ConversationId = last.ConversationId;
        response.ConversationId = last.ConversationId;
        interactions.Add(request);
        interactions.Add(response);

        return Task.CompletedTask;
    }

    public Task CompleteGenerationAsync(Interaction response)
    {
        base.ValidateInteractionForCompleteGeneration(response);
        // already mutated
        return Task.CompletedTask;
    }

    public Task ChangeConversationTopicAsync(Interaction changeTopic, Duration expiry)
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

    public Task ClearFeedbackAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task ClearFeedbackAsync(string userId, string activityId)
    {
        throw new NotImplementedException();
    }

    public Task CommentOnMessageAsync(string userId, string comment)
    {
        throw new NotImplementedException();
    }

    public Task CommentOnMessageAsync(string userId, string activityId, string comment)
    {
        throw new NotImplementedException();
    }

    public Task DeleteLastInteractionsAsync(string userId, int count = 1)
    {
        throw new NotImplementedException();
    }

    public Task<Conversation> GetCurrentConversationAsync(string userId)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? interactions))
        {
            return Task.FromResult(new Conversation { Interactions = [] });
        }

        var last = interactions.Last();
        var filtered = interactions
            .Where(x => x.ConversationId == last.ConversationId)
            .Where(x => x.State == States.EDITED || x.State == States.STOPPED || x.State == States.UNMODIFIED);
        return Task.FromResult(new Conversation { Interactions = [.. interactions] });
    }

    public Task RateMessageAsync(string userId, string rating)
    {
        throw new NotImplementedException();
    }

    public Task RateMessageAsync(string userId, string activityId, string rating)
    {
        throw new NotImplementedException();
    }

    public Task StartupAsync()
    {
        this.logger.LogInformation("starting up LocalMemoryHistoryService...");
        this.logger.LogInformation("successfully started up LocalMemoryHistoryService.");
        return Task.CompletedTask;
    }

    public Task SetCustomInstructionsAsync(string userId, string prompt)
    {
        throw new NotImplementedException();
    }

    public Task DeleteCustomInstructionsAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetCustomInstructionsAsync(string userId)
    {
        throw new NotImplementedException();
    }
}