namespace Bots;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AdaptiveCards.Templating;
using Channels;
using DistributedChat;
using Grpc.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ChatBot(
    IConfig config,
    HistoryService historyService,
    BotChannel channel,
    ILogger<ChatBot> logger)
    : ActivityHandler
{
    private readonly IConfig config = config;
    private readonly HistoryService historyService = historyService;
    private readonly BotChannel channel = channel;
    private readonly ILogger<ChatBot> logger = logger;
    private readonly string cardJson = File.ReadAllText("./card.json");
    private readonly string chatId = System.Guid.NewGuid().ToString();

    private async Task<string> Dispatch(
        string chatId,
        string? activityId,
        string status,
        string reply,
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        var template = new AdaptiveCardTemplate(cardJson);
        var isGenerated = status == "generated.";
        var data = new { chatId, status, reply, showFeedback = isGenerated, showStop = !isGenerated };
        var attachment = new Attachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = JsonConvert.DeserializeObject(template.Expand(data)),
        };
        var activity = MessageFactory.Attachment(attachment);

        if (string.IsNullOrEmpty(activityId))
        {
            var response = await turnContext.SendActivityAsync(activity, cancellationToken);
            return response.Id;
        }

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
        this.logger.LogInformation("User {user} sent: {text}", userId, text);

        // get the history
        this.historyService.Add(userId, "user", text);
        var request = new ChatRequest();
        foreach (var turn in this.historyService.Get(userId))
        {
            request.Turns.Add(turn);
        }

        // send the typing indicator
        await turnContext.SendActivityAsync(new Microsoft.Bot.Schema.Activity { Type = ActivityTypes.Typing }, cancellationToken);

        // prepare to receive the async response
        string? activityId = null;
        StringBuilder summaries = new();
        int lastSentAtLength = 0;

        // send the request
        using var streamingCall = this.channel.Client.Chat(request, cancellationToken: cancellationToken);

        // start the counters
        Stopwatch firstSw = new();
        Stopwatch lastSw = new();
        lastSw.Start();
        firstSw.Start();
        int totalWordCount = 0;

        // start receiving the async responses
        await foreach (var response in streamingCall.ResponseStream.ReadAllAsync(cancellationToken))
        {
            int wordCount = !string.IsNullOrEmpty(response.Msg)
                ? response.Msg.Replace('\n', ' ').Split(' ').Length
                : 0;
            totalWordCount += wordCount;
            if (wordCount > 0 && firstSw.IsRunning)
            {
                firstSw.Stop();
                DiagnosticService.RecordTimeToFirstResponse(firstSw.ElapsedMilliseconds, wordCount);
            }
            summaries.Append(response.Msg);
            if (summaries.Length - lastSentAtLength > this.config.CHARACTERS_PER_UPDATE)
            {
                lastSentAtLength = summaries.Length;
                activityId = await Dispatch(this.chatId, activityId, "generating...", summaries.ToString(), turnContext, cancellationToken);
            }
        }
        lastSw.Stop();
        DiagnosticService.RecordTimeToLastResponse(lastSw.ElapsedMilliseconds, totalWordCount);

        // dispatch the final response
        await Dispatch(this.chatId, activityId, "generated.", summaries.ToString(), turnContext, cancellationToken);
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
