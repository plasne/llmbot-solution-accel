namespace Bots;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AdaptiveCards.Templating;
using Channels;
using DistributedChat;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ChatBot(
    IHttpContextAccessor httpContextAccessor,
    IConfig config,
    HistoryService historyService,
    BotChannel channel,
    ILogger<ChatBot> logger)
    : ActivityHandler
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly IConfig config = config;
    private readonly HistoryService historyService = historyService;
    private readonly BotChannel channel = channel;
    private readonly ILogger<ChatBot> logger = logger;
    private readonly string cardJson = File.ReadAllText("./card.json");
    private readonly string chatId = System.Guid.NewGuid().ToString();

    public static string StartTimeKey = "http-request-start-time";

    private async Task<string> Dispatch(
        string chatId,
        string? activityId,
        string status,
        string reply,
        List<Citation>? citations,
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        // add citations
        if (citations is not null)
        {
            foreach (var citation in citations)
            {
                if (!string.IsNullOrEmpty(citation.Title) && !string.IsNullOrEmpty(citation.Uri))
                {
                    reply = reply.Replace($"[{citation.Ref}]", $"[[{citation.Title}]]({citation.Uri})");
                }
                else if (!string.IsNullOrEmpty(citation.Title))
                {
                    reply = reply.Replace($"[{citation.Ref}]", $"[{citation.Title}]");
                }
                else if (!string.IsNullOrEmpty(citation.Uri))
                {
                    reply = reply.Replace($"[{citation.Ref}]", $"[{citation.Ref}]({citation.Uri})");
                }
            }
        }

        // build the adaptive card
        var template = new AdaptiveCardTemplate(cardJson);
        var isGenerated = status == "Generated.";
        var data = new { chatId, status, reply, showFeedback = isGenerated, showStop = !isGenerated };
        var attachment = new Attachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = JsonConvert.DeserializeObject(template.Expand(data)),
        };
        var activity = MessageFactory.Attachment(attachment);

        // send the activity if new
        if (string.IsNullOrEmpty(activityId))
        {
            var response = await turnContext.SendActivityAsync(activity, cancellationToken);
            return response.Id;
        }

        // update instead
        activity.Id = activityId;
        await turnContext.UpdateActivityAsync(activity, cancellationToken);
        return activityId;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        // identify the user
        var userId = turnContext.Activity.From.AadObjectId;

        // look for ratings
        var jaction = turnContext.Activity.Value as JObject;
        var action = jaction?.ToObject<UserAction>();
        if (action is not null)
        {
            this.logger.LogInformation("User {user} rated {id} as {value}", userId, action.ChatId, action.Rate);
            var activity = MessageFactory.Text($"Thank you for rating '{action.Rate}' on chat '{action.ChatId}'.");
            await turnContext.SendActivityAsync(activity, cancellationToken);
            return;
        }

        // get the text
        var text = turnContext.Activity.Text;

        // get the history
        this.historyService.Add(userId, "user", text);
        var request = new ChatRequest { MinCharsToStream = this.config.CHARACTERS_PER_UPDATE };
        foreach (var turn in this.historyService.Get(userId))
        {
            request.Turns.Add(turn);
        }

        // send the typing indicator
        await turnContext.SendActivityAsync(new Microsoft.Bot.Schema.Activity { Type = ActivityTypes.Typing }, cancellationToken);

        // prepare to receive the async response
        string? activityId = null;
        StringBuilder summaries = new();
        var citations = new Dictionary<string, Citation>();

        // send the request
        using var streamingCall = this.channel.Client.Chat(request, cancellationToken: cancellationToken);

        // start the counters
        DateTime? started = null;
        if (httpContextAccessor.HttpContext is not null &&
            httpContextAccessor.HttpContext.Items.TryGetValue(StartTimeKey, out var requestStartTime) && requestStartTime is DateTime start)
        {
            started = start;
        }
        int totalWordCount = 0;

        // start receiving the async responses
        await foreach (var response in streamingCall.ResponseStream.ReadAllAsync(cancellationToken))
        {
            if (!string.IsNullOrEmpty(response.Msg))
            {
                int wordCount = response.Msg.Replace('\n', ' ').Split(' ').Length;
                totalWordCount += wordCount;
                if (started is not null && totalWordCount == wordCount)
                {
                    DiagnosticService.RecordTimeToFirstResponse((DateTime.UtcNow - started.Value).TotalMilliseconds);
                }
                summaries.Append(response.Msg);
            }
            if (response.Citations is not null)
            {
                foreach (var citation in response.Citations)
                {
                    citations.TryAdd(citation.Ref, citation);
                }
            }
            activityId = await Dispatch(
                this.chatId,
                activityId,
                response.Status,
                summaries.ToString(),
                response.Citations?.ToList(),
                turnContext,
                cancellationToken);
        }
        if (started is not null)
        {
            DiagnosticService.RecordTimeToLastResponse((DateTime.UtcNow - started.Value).TotalMilliseconds, totalWordCount);
        }
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeText = "Hello and welcome!";
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
            }
        }
    }
}
