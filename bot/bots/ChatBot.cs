namespace Bots;

using System;
using System.Collections.Generic;
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
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class ChatBot(
    IConfig config,
    IServiceProvider serviceProvider,
    ICardProvider cardProvider,
    HistoryService historyService,
    BotChannel channel,
    ILogger<ChatBot> logger)
    : ActivityHandler
{
    private readonly IConfig config = config;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ICardProvider cardProvider = cardProvider;
    private readonly HistoryService historyService = historyService;
    private readonly BotChannel channel = channel;
    private readonly ILogger<ChatBot> logger = logger;
    private readonly string chatId = System.Guid.NewGuid().ToString();

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
        var template = await cardProvider.GetTemplate("response");
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

    private static bool IsCommand(ITurnContext<IMessageActivity> turnContext)
    {
        if (turnContext.Activity.Value is not null)
        {
            return true;
        }
        if (turnContext.Activity.Text is not null && turnContext.Activity.Text.StartsWith('/'))
        {
            return true;
        }
        return false;
    }

    private async Task<bool> TryAsCommand(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        if (!IsCommand(turnContext))
        {
            return false;
        }

        // try each command until you find one that works
        var commands = this.serviceProvider.GetServices<ICommand>();
        foreach (var command in commands)
        {
            var handled = await command.Try(turnContext, cancellationToken);
            if (handled)
            {
                return true;
            }
        }

        // try the help command if nothing else found
        var helpCommand = commands.OfType<HelpCommand>().FirstOrDefault();
        helpCommand?.ShowHelp(turnContext, cancellationToken);
        return true;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        // see if this is a command
        if (await TryAsCommand(turnContext, cancellationToken))
        {
            return;
        }

        // get the text
        var text = turnContext.Activity.Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // get the history
        var userId = turnContext.Activity.From.AadObjectId;
        this.historyService.Add(userId, "user", text);
        var request = new ChatRequest { MinCharsToStream = this.config.CHARACTERS_PER_UPDATE };
        foreach (var turn in this.historyService.Get(userId))
        {
            request.Turns.Add(turn);
        }

        // send the typing indicator
        await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

        // prepare to receive the async response
        string? activityId = null;
        StringBuilder summaries = new();
        var citations = new Dictionary<string, Citation>();

        // send the request
        using var streamingCall = this.channel.Client.Chat(request, cancellationToken: cancellationToken);

        // start receiving the async responses
        await foreach (var response in streamingCall.ResponseStream.ReadAllAsync(cancellationToken))
        {
            if (!string.IsNullOrEmpty(response.Msg))
            {
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
