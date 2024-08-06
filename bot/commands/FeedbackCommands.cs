using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Shared.Models.Memory;

namespace Bot;

public class FeedbackCommands(IConfig config, ILogger<FeedbackCommands> logger, IHttpClientFactory httpClientFactory) : ICommands
{
    private readonly IConfig config = config;
    private readonly ILogger<FeedbackCommands> logger = logger;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

    public Dictionary<string, string> Commands => new()
    {
        { "/feedback", "shows your rating and comments for the last response from the bot." },
        { "/rate up", "rates the last response from the bot as positive." },
        { "/rate down", "rates the last response from the bot as negative." },
        { "/rate-comment <rating_comment>", "allows you to give feedback comments on the last response from the bot."  },
        { "/revoke", "to clear your rating and comments for the last response from the bot."  },
    };

    public async Task<bool> Try(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default)
    {
        var userId = turnContext.Activity.From.AadObjectId;

        if (turnContext.Activity.Value is JObject jaction && jaction.ToObject<UserAction>() is UserAction action && !string.IsNullOrEmpty(action.ActivityId))
        {
            // Check the result here, if it's true, return true. Otherwise, continue to the next step.
            var result = await HandleActionAsync(userId, action, turnContext, cancellationToken);
            if (result) { return true; }
        }

        var text = turnContext.Activity.Text?.ToLower();
        if (text != null)
        {
            return await HandleCommandAsync(userId, text, turnContext, cancellationToken);
        }

        return false;
    }

    private async Task<bool> HandleActionAsync(string userId, UserAction action, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        switch (action.Command)
        {
            case "/rate up":
#pragma warning disable CS8604 // Possible null reference argument.
                await RateAsync(userId, action.ActivityId, "up", turnContext, cancellationToken);
#pragma warning restore CS8604 // Possible null reference argument.
                return true;
            case "/rate down":
#pragma warning disable CS8604 // Possible null reference argument.
                await RateAsync(userId, action.ActivityId, "down", turnContext, cancellationToken);
#pragma warning restore CS8604 // Possible null reference argument.
                return true;
            case "/rate-comment":
                await RateCommentAsync(userId, action, turnContext, cancellationToken);
                return true;
            default:
                return false;
        }
    }

    private async Task<bool> HandleCommandAsync(string userId, string text, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        switch (text)
        {
            case "/feedback":
                await ShowFeedbackLastAsync(userId, turnContext, cancellationToken);
                return true;
            case "/rate up":
                await RateLastAsync(userId, "up", turnContext, cancellationToken);
                return true;
            case "/rate down":
                await RateLastAsync(userId, "down", turnContext, cancellationToken);
                return true;
            case "/revoke":
                await RevokeLastAsync(userId, turnContext, cancellationToken);
                return true;
            default:
                if (text.StartsWith("/rate-comment"))
                {
                    var comment = text.Substring("/rate-comment".Length).Trim();
                    if (!string.IsNullOrEmpty(comment))
                    {
                        await RateCommentLastAsync(userId, comment, turnContext, cancellationToken);
                        return true;
                    }
                    await SendActivityAsync(turnContext, "Please provide a comment for the rating.", cancellationToken);
                }
                return false;
        }
    }

    private async Task RevokeLastAsync(string userId, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} revoked last rating", userId);
        await SendHttpRequestAsync(HttpMethod.Delete, $"{config.MEMORY_URL}/api/users/{userId}/activities/:last/feedback", cancellationToken);
        await SendActivityAsync(turnContext, "Your rating for the previous chat has been revoked.", cancellationToken);
    }

    private async Task RateCommentAsync(string userId, UserAction action, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        if (action.Comment == null)
        {
            await SendActivityAsync(turnContext, "Please provide a comment for the rating.", cancellationToken);
            return;
        }
#pragma warning disable CS8604 // Possible null reference argument.
        await RateCommentAsync(userId, action.ActivityId, action.Comment, turnContext, cancellationToken);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    private async Task RateCommentAsync(string userId, string activityId, string feedback, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} left feedback for {id}.", userId, activityId);
        var content = new FeedbackRequest { Comment = feedback }.ToJsonContent();
        await SendHttpRequestAsync(HttpMethod.Put, $"{config.MEMORY_URL}/api/users/{userId}/activities/{activityId}/feedback", cancellationToken, content);
        await SendActivityAsync(turnContext, $"Thank you for leaving feedback on chat '{activityId}'.", cancellationToken);
    }

    private async Task RateCommentLastAsync(string userId, string feedback, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} left feedback for last chat.", userId);
        var content = new FeedbackRequest { Comment = feedback }.ToJsonContent();
        await SendHttpRequestAsync(HttpMethod.Put, $"{config.MEMORY_URL}/api/users/{userId}/activities/:last/feedback", cancellationToken, content);
        await SendActivityAsync(turnContext, "Thank you for leaving feedback on last chat.", cancellationToken);
    }

    private async Task RateAsync(string userId, string activityId, string value, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} rated {id} as {value}", userId, activityId, value);
        var content = new FeedbackRequest { Rating = value }.ToJsonContent();
        await SendHttpRequestAsync(HttpMethod.Put, $"{config.MEMORY_URL}/api/users/{userId}/activities/{activityId}/feedback", cancellationToken, content);
        await SendActivityAsync(turnContext, $"Thank you for rating '{value}' on chat '{activityId}'.", cancellationToken);
    }

    private async Task RateLastAsync(string userId, string value, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} rated last message as {value}", userId, value);
        var content = new FeedbackRequest { Rating = value }.ToJsonContent();
        await SendHttpRequestAsync(HttpMethod.Put, $"{config.MEMORY_URL}/api/users/{userId}/activities/:last/feedback", cancellationToken, content);
        await SendActivityAsync(turnContext, $"Thank you for rating '{value}' on last chat.", cancellationToken);
    }

    private async Task ShowFeedbackLastAsync(string userId, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} requested feedback", turnContext.Activity.From.AadObjectId);
        var feedback = await GetFeedbackAsync($"{config.MEMORY_URL}/api/users/{userId}/activities/:last/feedback", cancellationToken);
        await SendActivityAsync(turnContext, $"You rated '{feedback.Rating}' with comment '{feedback.Comment}'.", cancellationToken);
    }

    private async Task SendActivityAsync(ITurnContext<IMessageActivity> turnContext, string message, CancellationToken cancellationToken)
    {
        var activity = MessageFactory.Text(message);
        await turnContext.SendActivityAsync(activity, cancellationToken);
    }

    private async Task SendHttpRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken, HttpContent? content = null)
    {
        using var httpClient = httpClientFactory.CreateClient("retry");
        var request = new HttpRequestMessage(method, url) { Content = content };
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"The attempt to {method} {url} resulted in HTTP {response.StatusCode} - {errorContent}.");
        }
    }

    private async Task<FeedbackRequest> GetFeedbackAsync(string url, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient("retry");
        var response = await httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"The attempt to GET {url} resulted in HTTP {response.StatusCode} - {errorContent}.");
        }

        var result = await response.Content.ReadFromJsonAsync<FeedbackRequest>(cancellationToken);
        if (result == null)
        {
            throw new Exception("Failed to deserialize the response into FeedbackRequest.");
        }

        return result;
    }
}