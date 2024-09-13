using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Models.Memory;

namespace Bot;

public class FeedbackCommands(IConfig config, ILogger<FeedbackCommands> logger, ICardProvider cardProvider, IHttpClientFactory httpClientFactory) : ICommands
{
    private readonly IConfig config = config;
    private readonly ILogger<FeedbackCommands> logger = logger;
    private readonly ICardProvider cardProvider = cardProvider;
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
            var result = await this.HandleActionAsync(userId, action, turnContext, cancellationToken);
            if (result)
            { return true; }
        }

        var text = turnContext.Activity.Text?.ToLower();
        if (text != null)
        {
            return await this.HandleCommandAsync(userId, text, turnContext, cancellationToken);
        }

        return false;
    }

    private async Task<bool> HandleActionAsync(string userId, UserAction action, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        switch (action.Command)
        {
            case "/rate up":
                await this.RateAsync(userId, action, "up", turnContext, cancellationToken);
                return true;
            case "/rate down":
                await this.RateAsync(userId, action, "down", turnContext, cancellationToken);
                return true;
            case "/rate-comment":
                await this.RateCommentAsync(userId, action, turnContext, cancellationToken);
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
                await this.ShowFeedbackLastAsync(userId, turnContext, cancellationToken);
                return true;
            case "/rate up":
                await this.RateLastAsync(userId, "up", turnContext, cancellationToken);
                return true;
            case "/rate down":
                await this.RateLastAsync(userId, "down", turnContext, cancellationToken);
                return true;
            case "/revoke":
                await this.RevokeLastAsync(userId, turnContext, cancellationToken);
                return true;
            default:
                if (text.StartsWith("/rate-comment"))
                {
                    var comment = text.Substring("/rate-comment".Length).Trim();
                    if (!string.IsNullOrEmpty(comment))
                    {
                        await this.RateCommentLastAsync(userId, comment, turnContext, cancellationToken);
                        return true;
                    }
                    await SendActivityAsync(turnContext, "Please provide a comment for the rating. ex. (`/rate-comment I liked this response.`)", cancellationToken);
                }
                return false;
        }
    }

    private async Task RevokeLastAsync(string userId, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("User {u} is attempting to revoke their last rating...", userId);
            await this.SendHttpRequestAsync(HttpMethod.Delete, $"{config.MEMORY_URL}/api/users/{userId}/activities/:last/feedback", cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("User {u} attempted to revoke their last rating, but the chat was not found.", userId);
            await SendActivityAsync(turnContext, "I'm sorry, last chat was previously deleted. Feedback no longer exists.", cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "User {u} attempted to revoke their last rating, but it failed.", userId);
            await SendActivityAsync(turnContext, "Failed to revoke last rating.", cancellationToken);
            return;
        }
        logger.LogInformation("User {u} has successfully revoked their last rating.", userId);
        await SendActivityAsync(turnContext, "Your rating for the last chat has been revoked.", cancellationToken);
    }

    private async Task RateCommentAsync(string userId, UserAction action, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        if (action.Comment == null)
        {
            await SendActivityAsync(turnContext, "Please provide a comment for the rating.", cancellationToken);
            return;
        }
#pragma warning disable CS8604 // Possible null reference argument.
        await this.RateCommentAsync(userId, action.ActivityId, action.Comment, turnContext, cancellationToken);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    private async Task RateCommentAsync(string userId, string activityId, string feedback, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} left feedback for {id}.", userId, activityId);
        var content = new FeedbackRequest { Comment = feedback }.ToJsonContent();
        await this.SendHttpRequestAsync(HttpMethod.Put, $"{config.MEMORY_URL}/api/users/{userId}/activities/{activityId.ToBase64()}/feedback", cancellationToken, content);
        await SendActivityAsync(turnContext, $"Thank you for leaving feedback.", cancellationToken);
    }

    private async Task RateCommentLastAsync(string userId, string feedback, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var content = new FeedbackRequest { Comment = feedback }.ToJsonContent();
        try
        {
            logger.LogDebug("User {u} is attempting to leave feedback for last chat...", userId);
            await this.SendHttpRequestAsync(HttpMethod.Put, $"{config.MEMORY_URL}/api/users/{userId}/activities/:last/feedback", cancellationToken, content);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("User {u} attempted to leave feedback for last chat, but the chat was not found.", userId);
            await SendActivityAsync(turnContext, "I'm sorry, last chat was previously deleted. It is not available for commenting.", cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "User {u} attempted to leave feedback for last chat, but it failed.", userId);
            await SendActivityAsync(turnContext, "Failed to leave feedback for last chat.", cancellationToken);
            return;
        }
        logger.LogInformation("User {u} has successfully left feedback for last chat.", userId);
        await SendActivityAsync(turnContext, "Thank you for leaving feedback on last chat.", cancellationToken);
    }

    private async Task RateAsync(string userId, UserAction action, string value, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} rated {id} as {value}", userId, action.ActivityId, value);
        var activityId = action.ActivityId!;

        // get the feedback for the activity
        var feedback = await this.GetFeedbackAsync($"{config.MEMORY_URL}/api/users/{userId}/activities/{activityId.ToBase64()}/feedback", cancellationToken);

        // update the adaptive card with the reply and citation
        var template = await cardProvider.GetTemplate("response");
        string reply = feedback?.Message ?? "";
        var citations = new List<Citation>();
        if (!string.IsNullOrEmpty(feedback?.Citations))
        {
            citations = JsonConvert.DeserializeObject<List<Citation>>(feedback.Citations) ?? new List<Citation>();
        }
        Func<string, string> Expand = (msg) =>
        {
            var data = new
            {
                activityId,
                status = config.FINAL_STATUS,
                reply = msg,
                citations,
                showFeedback = true,
                showStop = false,
                showDelete = true,
                showUpVoteSelected = "up" == value,
                showDownVoteSelected = "down" == value
            };
            return template.Expand(data);
        };
        var cardContentWithReplyAndCitations = Expand(reply);
        if (cardContentWithReplyAndCitations.Length > this.config.MAX_PAYLOAD_SIZE)
        {
            var truncated = reply.Truncate(cardContentWithReplyAndCitations.Length, this.config.MAX_PAYLOAD_SIZE);
            cardContentWithReplyAndCitations = Expand(truncated);
        }
        var attachmentWithReplyAndCitations = new Attachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = JsonConvert.DeserializeObject(cardContentWithReplyAndCitations),
        };
        await UpdateActivityAsync(activityId, attachmentWithReplyAndCitations, turnContext, cancellationToken);
        var content = new FeedbackRequest { Rating = value }.ToJsonContent();
        await this.SendHttpRequestAsync(HttpMethod.Put, $"{config.MEMORY_URL}/api/users/{userId}/activities/{activityId.ToBase64()}/feedback", cancellationToken, content);
        await SendActivityAsync(turnContext, $"Thank you for rating '{value}' on the chat.", cancellationToken);
    }

    private async Task RateLastAsync(string userId, string value, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var content = new FeedbackRequest { Rating = value }.ToJsonContent();
        try
        {
            logger.LogDebug("User {user} is attempting to rate last chat as {value}...", userId, value);
            await this.SendHttpRequestAsync(HttpMethod.Put, $"{config.MEMORY_URL}/api/users/{userId}/activities/:last/feedback", cancellationToken, content);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("User {user} attempted to rate last chat as {value}, but the chat was not found.", userId, value);
            await SendActivityAsync(turnContext, "I'm sorry, last chat was previously deleted. It is not available for rating.", cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "User {user} attempted to rate last chat as {value}, but it failed.", userId, value);
            await SendActivityAsync(turnContext, "Failed to rate last chat.", cancellationToken);
            return;
        }
        logger.LogInformation("User {user} has successfully rated last chat as {value}.", userId, value);
        await SendActivityAsync(turnContext, $"Thank you for rating '{value}' on last chat.", cancellationToken);
    }

    private async Task ShowFeedbackLastAsync(string userId, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {user} requested feedback", turnContext.Activity.From.AadObjectId);
        try
        {
            var feedback = await this.GetFeedbackAsync($"{config.MEMORY_URL}/api/users/{userId}/activities/:last/feedback", cancellationToken);
            string message;
            if (string.IsNullOrEmpty(feedback.Rating) && string.IsNullOrEmpty(feedback.Comment))
            {
                message = "Feedback for last chat was not found.";
            }
            else if (string.IsNullOrEmpty(feedback.Rating))
            {
                message = $"You commented '{feedback.Comment}' on the last chat.";
            }
            else if (string.IsNullOrEmpty(feedback.Comment))
            {
                message = $"You rated '{feedback.Rating}' on the last chat.";
            }
            else
            {
                message = $"You rated '{feedback.Rating}' with comment '{feedback.Comment}'.";
            }
            await SendActivityAsync(turnContext, message, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("User {u} attempted to get feedback for last chat, but the chat was not found.", userId);
            await SendActivityAsync(turnContext, "I'm sorry, last chat was previously deleted. Feedback no longer exists.", cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "User {u} attempted to get feedback for last chat, but it failed.", userId);
            await SendActivityAsync(turnContext, "Failed to get feedback for last chat.", cancellationToken);
            return;
        }
    }

    private async static Task UpdateActivityAsync(string activityId, Attachment attachment, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var activity = MessageFactory.Attachment(attachment);
        activity.Id = activityId;
        await turnContext.UpdateActivityAsync(activity, cancellationToken);
    }

    private async static Task SendActivityAsync(ITurnContext<IMessageActivity> turnContext, string message, CancellationToken cancellationToken)
    {
        var activity = MessageFactory.Text(message);
        await turnContext.SendActivityAsync(activity, cancellationToken);
    }

    private async Task SendHttpRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken, HttpContent? content = null)
    {
        using var httpClient = httpClientFactory.CreateClient("retry");
        var request = new HttpRequestMessage(method, url) { Content = content };
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<FeedbackRequest> GetFeedbackAsync(string url, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient("retry");
        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FeedbackRequest>(cancellationToken);
        if (result == null)
        {
            throw new Exception("Failed to deserialize the response into FeedbackRequest.");
        }

        return result;
    }
}