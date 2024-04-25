namespace Bots;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Channels;
using DistributedChat;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class ChatBot(
    IHttpContextAccessor httpContextAccessor,
    IConfig config,
    IServiceProvider serviceProvider,
    ICardProvider cardProvider,
    HistoryService historyService,
    BotChannel channel,
    ILogger<ChatBot> logger)
    : ActivityHandler
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly IConfig config = config;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ICardProvider cardProvider = cardProvider;
    private readonly HistoryService historyService = historyService;
    private readonly BotChannel channel = channel;
    private readonly ILogger<ChatBot> logger = logger;
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
        var template = await cardProvider.GetTemplate("response");
        var isGenerated = status == this.config.FINAL_STATUS;
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
        var commands = this.serviceProvider.GetServices<ICommands>();
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

        // start receiving the async responses
        var initMsgResponse = false;
        await foreach (var response in streamingCall.ResponseStream.ReadAllAsync(cancellationToken))
        {
            var status = response.Status;

            // append the summary with any message
            if (!string.IsNullOrEmpty(response.Msg))
            {
                if (started is not null && !initMsgResponse)
                {
                    initMsgResponse = true;
                    DiagnosticService.RecordTimeToFirstResponse((DateTime.UtcNow - started.Value).TotalMilliseconds);
                }
                summaries.Append(response.Msg);
            }

            // add any citations that were found in the response
            if (response.Citations is not null)
            {
                foreach (var citation in response.Citations)
                {
                    citations.TryAdd(citation.Ref, citation);
                }
            }

            // the LLM may have determined the user's intent is something other than what the LLM can provide
            this.logger.LogWarning("intent is {intent}", response.Intent);
            switch (response.Intent)
            {
                case Intent.Unset:
                case Intent.InDomain:
                    // no need to do anything, these are good intents
                    break;
                case Intent.Unknown:
                case Intent.Greeting:
                    status = this.config.FINAL_STATUS;
                    summaries.Clear();
                    summaries.Append("Hello and welcome! If you aren't sure what I can do type `/help`.");
                    break;
                case Intent.OutOfDomain:
                    status = this.config.FINAL_STATUS;
                    summaries.Clear();
                    summaries.Append("I'm sorry, I can't help with that. If you aren't sure what I can do type `/help`.");
                    break;
                case Intent.Goodbye:
                    status = this.config.FINAL_STATUS;
                    summaries.Clear();
                    summaries.Append("Goodbye!");
                    break;
                case Intent.TopicChange:
                    status = this.config.FINAL_STATUS;
                    summaries.Clear();
                    summaries.Append("Changing topic..."); // TODO: implement and consider message
                    break;
            }

            // dispatch the response
            activityId = await Dispatch(
                this.chatId,
                activityId,
                status,
                summaries.ToString(),
                response.Citations?.ToList(),
                turnContext,
                cancellationToken);
        }
        if (started is not null)
        {
            DiagnosticService.RecordTimeToLastResponse((DateTime.UtcNow - started.Value).TotalMilliseconds);
        }
    }

    protected override Task OnMessageUpdateActivityAsync(ITurnContext<IMessageUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        // TODO: implement this functionality
        this.logger.LogWarning("OnMessageUpdateActivityAsync, id: {i}, new msg: {t}", turnContext.Activity.Id, turnContext.Activity.Text);
        return base.OnMessageUpdateActivityAsync(turnContext, cancellationToken);
    }

    protected override Task OnMessageDeleteActivityAsync(ITurnContext<IMessageDeleteActivity> turnContext, CancellationToken cancellationToken)
    {
        // TODO: implement this functionality
        this.logger.LogWarning("OnMessageDeleteActivityAsync, id: {i}", turnContext.Activity.Id);
        return base.OnMessageDeleteActivityAsync(turnContext, cancellationToken);
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
