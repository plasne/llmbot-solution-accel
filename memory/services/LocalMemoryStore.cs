using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iso8601DurationHelper;
using Shared.Models;
using Shared;
using Shared.Models.Memory;
using System.Collections.Concurrent;

namespace Memory;

public class LocalMemoryStore() : MemoryStoreBase, IMemoryStore
{
    private readonly ConcurrentDictionary<string, List<Interaction>> interactions = [];
    private readonly ConcurrentDictionary<string, CustomInstructions> customInstructions = [];

    public Task<Guid> StartGenerationAsync(Interaction request, Interaction response, Duration expiry, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForStartGeneration(request);
        base.ValidateInteractionForStartGeneration(response);

        this.interactions.AddOrUpdate(request.UserId!, key =>
        {
            var conversationId = Guid.NewGuid();
            request.ConversationId = conversationId;
            response.ConversationId = conversationId;
            return [request, response];
        }, (key, list) =>
        {
            var last = list[^1];
            if (last.State == States.GENERATING)
            {
                throw new HttpException(423, "a response is already being generated.");
            }

            request.ConversationId = last.ConversationId;
            response.ConversationId = last.ConversationId;
            list.Add(request);
            list.Add(response);
            return list;
        });

        return Task.FromResult(request.ConversationId);
    }

    public Task CompleteGenerationAsync(Interaction response, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForCompleteGeneration(response);

        if (!this.interactions.TryGetValue(response.UserId!, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the specified interaction for user ID '{response.UserId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.ActivityId == response.ActivityId);
        if (last is null)
        {
            throw new HttpException(404, $"the specified interaction for user ID '{response.UserId}' was not found.");
        }

        last.ConversationId = response.ConversationId;
        last.Message = response.Message;
        last.Citations = response.Citations;
        last.State = response.State;
        last.Intent = response.Intent;
        last.PromptTokenCount = response.PromptTokenCount;
        last.CompletionTokenCount = response.CompletionTokenCount;
        last.EmbeddingTokenCount = response.EmbeddingTokenCount;
        last.TimeToFirstResponse = response.TimeToFirstResponse;
        last.TimeToLastResponse = response.TimeToLastResponse;

        return Task.CompletedTask;
    }

    public Task ChangeConversationTopicAsync(Interaction changeTopic, Duration expiry, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForTopicChange(changeTopic);
        this.interactions.AddOrUpdate(changeTopic.UserId!, key =>
        {
            return [changeTopic];
        }, (key, list) =>
        {
            list.Add(changeTopic);
            return list;
        });
        return Task.CompletedTask;
    }

    public Task ClearLastFeedbackAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the last interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.Role == Roles.ASSISTANT && x.State != States.DELETED);
        if (last is null)
        {
            throw new HttpException(404, $"the last interaction for user ID '{userId}' was not found.");
        }

        last.Rating = null;
        last.Comment = null;
        return Task.CompletedTask;
    }

    public Task ClearFeedbackAsync(string userId, string activityId, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.ActivityId == activityId && x.Role == Roles.ASSISTANT);
        if (last is null)
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        last.Rating = null;
        last.Comment = null;
        return Task.CompletedTask;
    }

    public Task CommentOnLastMessageAsync(string userId, string comment, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the last interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.Role == Roles.ASSISTANT && x.State != States.DELETED);
        if (last is null)
        {
            throw new HttpException(404, $"the last interaction for user ID '{userId}' was not found.");
        }

        last.Comment = comment;
        return Task.CompletedTask;
    }

    public Task CommentOnMessageAsync(string userId, string activityId, string comment, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.ActivityId == activityId && x.Role == Roles.ASSISTANT);
        if (last is null)
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        last.Comment = comment;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DeletedUserMessage>> DeleteActivitiesAsync(string userId, int count = 1, CancellationToken cancellationToken = default)
    {
        var deleted = new List<DeletedUserMessage>();

        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            return Task.FromResult(deleted.AsEnumerable());
        }

        var lastInteractions = list.AsEnumerable().Reverse().Take(count);
        foreach (var interaction in lastInteractions)
        {
            deleted.Add(new DeletedUserMessage
            {
                ActivityId = interaction.ActivityId!,
                Role = interaction.Role
            });
            interaction.State = States.DELETED;
            interaction.Message = null;
            interaction.Citations = null;
            interaction.Rating = null;
            interaction.Comment = null;
        }

        return Task.FromResult(deleted.AsEnumerable());
    }

    public Task DeleteActivityAsync(string userId, string activityId, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.ActivityId == activityId);
        if (last is null)
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        last.State = States.DELETED;
        last.Message = null;
        last.Citations = null;
        last.Rating = null;
        last.Comment = null;

        return Task.CompletedTask;
    }

    public Task<Conversation> GetLastConversationAsync(string userId, int? maxTokens, string? modelName, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation { Id = Guid.Empty, Turns = [] };
        var turns = new Stack<Turn>();

        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            return Task.FromResult(conversation);
        }

        var last = list[^1];
        var filtered = list
            .Where(x => x.ConversationId == last.ConversationId)
            .Where(x => x.State != States.DELETED)
            .Reverse();

        int totalTokenCount = 0;
        foreach (var interaction in filtered)
        {
            conversation.Id = interaction.ConversationId;
            var turn = new Turn { Role = interaction.Role, Msg = interaction.Message! };
            if (!string.IsNullOrWhiteSpace(turn.Msg))
            {
                if (modelName is not null && maxTokens is not null && IsMaxTokenLimitExceeded(modelName, maxTokens.Value, turn.Msg, ref totalTokenCount))
                {
                    break;
                }

                turns.Push(turn);
            }
        }

        conversation.Turns = turns.ToArray();
        return Task.FromResult(conversation);
    }

    public Task RateLastMessageAsync(string userId, string rating, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the last interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.Role == Roles.ASSISTANT && x.State != States.DELETED);
        if (last is null)
        {
            throw new HttpException(404, $"the last interaction for user ID '{userId}' was not found.");
        }

        last.Rating = rating;
        return Task.CompletedTask;
    }

    public Task RateMessageAsync(string userId, string activityId, string rating, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.ActivityId == activityId && x.Role == Roles.ASSISTANT);
        if (last is null)
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        last.Rating = rating;
        return Task.CompletedTask;
    }

    public Task<Interaction> GetLastInteractionAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the last interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.Role == Roles.ASSISTANT && x.State != States.DELETED);
        if (last is null)
        {
            throw new HttpException(404, $"the last interaction for user ID '{userId}' was not found.");
        }

        return Task.FromResult(last);
    }

    public Task<Interaction> GetInteractionAsync(string userId, string activityId, CancellationToken cancellationToken = default)
    {
        if (!this.interactions.TryGetValue(userId, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.ActivityId == activityId);
        if (last is null)
        {
            throw new HttpException(404, $"the specified interaction for user ID '{userId}' was not found.");
        }

        return Task.FromResult(last);
    }

    public Task SetCustomInstructionsAsync(string userId, CustomInstructions instructions, CancellationToken cancellationToken = default)
    {
        this.customInstructions.AddOrUpdate(userId, instructions, (key, value) => instructions);
        return Task.CompletedTask;
    }

    public Task DeleteCustomInstructionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        this.customInstructions.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    public Task<CustomInstructions> GetCustomInstructionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this.customInstructions.GetValueOrDefault(userId, new CustomInstructions { Prompt = string.Empty }));
    }

    public Task UpdateUserMessageAsync(Interaction response, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForUserMessage(response);

        if (!this.interactions.TryGetValue(response.UserId!, out List<Interaction>? list))
        {
            throw new HttpException(404, $"the specified interaction for user ID '{response.UserId}' was not found.");
        }

        var last = list.LastOrDefault(x => x.ActivityId == response.ActivityId && x.Role == Roles.USER);
        if (last is null)
        {
            throw new HttpException(404, $"the specified interaction for user ID '{response.UserId}' was not found.");
        }

        last.State = States.EDITED;
        last.Message = response.Message;
        return Task.CompletedTask;
    }
}