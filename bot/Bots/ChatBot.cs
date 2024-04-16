namespace Bots;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Channels;
using DistributedChat;
using Grpc.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

    private async Task<string> Dispatch(
        string? id,
        string status,
        string text,
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        // var activity = MessageFactory.Text(text, text);

        var valid = JsonConvert.ToString(text);
        var json = cardJson.Replace("${status}", status).Replace("\"${body}\"", valid);
        var attachment = new Attachment()
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JsonConvert.DeserializeObject(json),
        };
        var activity = MessageFactory.Attachment(attachment);

        if (string.IsNullOrEmpty(id))
        {
            var response = await turnContext.SendActivityAsync(activity, cancellationToken);
            return response.Id;
        }

        activity.Id = id;
        await turnContext.UpdateActivityAsync(activity, cancellationToken);
        return id;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        // look for ratings
        if (turnContext.Activity.Text.StartsWith("/rate"))
        {
            await turnContext.SendActivityAsync("Thanks for rating this response!", cancellationToken: cancellationToken);
            return;
        }

        // identify the user
        var userId = turnContext.Activity.From.AadObjectId;
        this.logger.LogInformation("User {user} sent: {text}", userId, turnContext.Activity.Text);

        // get the history
        this.historyService.Add(userId, "user", turnContext.Activity.Text);
        var request = new ChatRequest();
        foreach (var turn in this.historyService.Get(userId))
        {
            request.Turns.Add(turn);
        }

        // send the typing indicator
        await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

        // prepare to receive the async response
        string? id = null;
        StringBuilder summaries = new();
        int lastSentAtLength = 0;

        // send the request
        using var streamingCall = this.channel.Client.Chat(request, cancellationToken: cancellationToken);

        // start receiving the async responses
        await foreach (var response in streamingCall.ResponseStream.ReadAllAsync(cancellationToken))
        {
            summaries.Append(response.Msg);
            if (summaries.Length - lastSentAtLength > this.config.CHARACTERS_PER_UPDATE)
            {
                lastSentAtLength = summaries.Length;
                id = await Dispatch(id, "generating...", summaries.ToString(), turnContext, cancellationToken);
            }
        }

        // dispatch the final response
        await Dispatch(id, "generated.", summaries.ToString(), turnContext, cancellationToken);
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
