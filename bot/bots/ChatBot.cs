namespace Bots;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
    IConfig config,
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider,
    ICardProvider cardProvider,
    IHttpClientFactory httpClientFactory,
    BotChannel channel,
    ILogger<ChatBot> logger)
    : ActivityHandler
{
    private readonly IConfig config = config;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ICardProvider cardProvider = cardProvider;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly BotChannel channel = channel;
    private readonly ILogger<ChatBot> logger = logger;

    public static string StartTimeKey = "http-request-start-time";

    private async Task<string> Dispatch(
        string? activityId,
        bool useAdaptiveCard,
        string? status,
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

        // build the activity
        IMessageActivity activity;
        if (useAdaptiveCard)
        {
            // build the adaptive card
            var template = await cardProvider.GetTemplate("response");
            var isGenerated = status == this.config.FINAL_STATUS;
            var data = new { activityId, status, reply, showFeedback = isGenerated, showStop = !isGenerated };
            var attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(template.Expand(data)),
            };
            activity = MessageFactory.Attachment(attachment);
        }
        else
        {
            // reply as plain text
            activity = MessageFactory.Text(reply);
        }

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
        // start the counters
        var started = httpContextAccessor.HttpContext is not null
            && httpContextAccessor.HttpContext.Items.TryGetValue(StartTimeKey, out var requestStartTime)
            && requestStartTime is DateTime requestStart
            ? requestStart
            : DateTime.UtcNow;

        // see if this is a command
        if (await TryAsCommand(turnContext, cancellationToken))
        {
            return;
        }

        // get the text
        var text = turnContext.Activity.Text;
        if (string.IsNullOrEmpty(text))
        {
            throw new Exception("no text was found in the activity from the user.");
        }

        // get the user
        var userId = turnContext.Activity.From.AadObjectId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("no user identity was found in the activity from the user.");
        }

        // try all user requests; this is always a single item, except when there is a topic change with a follow-up question
        var userRequests = new Queue<string>();
        userRequests.Enqueue(text);
        while (userRequests.Count > 0)
        {
            var request = userRequests.Dequeue();

            // send the "connection" message
            var activityId = await Dispatch(null, true, "Connecting to assistant...", string.Empty, null, turnContext, cancellationToken);

            // once history is started, it needs to catch errors so it isn't stuck generating
            StringBuilder summaries = new();
            var citations = new Dictionary<string, Citation>();
            try
            {
                // start the conversation in history
                using var httpClient = this.httpClientFactory.CreateClient();
                var startGenerationRequest = new StartGenerationRequest(turnContext.Activity.Id, request, activityId);
                var startGenerationResponse = await httpClient.PutAsJsonAsync(
                    $"{this.config.MEMORY_URL}/api/users/{userId}/conversations/current/turns",
                    startGenerationRequest,
                    cancellationToken);
                startGenerationResponse.EnsureSuccessStatusCode();

                // botResponseInteraction.State = States.UNMODIFIED;

                // create the request
                var chatRequest = new ChatRequest
                {
                    UserId = userId,
                    MinCharsToStream = this.config.CHARACTERS_PER_UPDATE,
                };

                // send the request
                using var streamingCall = this.channel.Client.Chat(chatRequest, cancellationToken: cancellationToken);

                // start receiving the async responses
                await foreach (var chatResponse in streamingCall.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    var status = chatResponse.Status;

                    // append the summary with any message
                    if (!string.IsNullOrEmpty(chatResponse.Msg))
                    {
                        if (botResponseInteraction.TimeToFirstResponse == 0)
                        {
                            botResponseInteraction.TimeToFirstResponse = (int)(DateTime.UtcNow - started).TotalMilliseconds;
                            DiagnosticService.RecordTimeToFirstResponse(botResponseInteraction.TimeToFirstResponse);
                        }
                        summaries.Append(chatResponse.Msg);
                    }

                    // add any citations that were found in the response
                    if (chatResponse.Citations is not null)
                    {
                        foreach (var citation in chatResponse.Citations)
                        {
                            citations.TryAdd(citation.Ref, citation);
                        }
                    }

                    // add any token counts
                    botResponseInteraction.PromptTokenCount += chatResponse.PromptTokens;
                    botResponseInteraction.CompletionTokenCount += chatResponse.CompletionTokens;

                    // the LLM may have determined the user's intent is something other than what the LLM can provide
                    var useAdaptiveCard = true;
                    switch (chatResponse.Intent)
                    {
                        case Intent.Unset:
                            break;
                        case Intent.InDomain:
                            botResponseInteraction.Intent = Intents.IN_DOMAIN;
                            break;
                        case Intent.Unknown:
                        case Intent.Greeting:
                            botResponseInteraction.Intent = Intents.GREETING;
                            status = this.config.FINAL_STATUS;
                            summaries.ResetTo("Hello and welcome! If you aren't sure what I can do type `/help`.");
                            break;
                        case Intent.OutOfDomain:
                            botResponseInteraction.Intent = Intents.OUT_OF_DOMAIN;
                            status = this.config.FINAL_STATUS;
                            summaries.ResetTo("I'm sorry, I can't help with that. If you aren't sure what I can do type `/help`.");
                            break;
                        case Intent.Goodbye:
                            botResponseInteraction.Intent = Intents.GOODBYE;
                            status = this.config.FINAL_STATUS;
                            summaries.ResetTo("Goodbye!");
                            break;
                        case Intent.TopicChange:
                            botResponseInteraction.ConversationId = Guid.NewGuid();
                            botResponseInteraction.Intent = Intents.TOPIC_CHANGE;
                            botResponseInteraction.State = States.EMPTY;
                            if (!string.IsNullOrEmpty(chatResponse.Msg))
                            {
                                userRequests.Enqueue(chatResponse.Msg);
                            }
                            status = this.config.FINAL_STATUS;
                            useAdaptiveCard = false;
                            summaries.ResetTo("Let's start a new conversation.");
                            break;
                    }

                    // dispatch the response
                    activityId = await Dispatch(
                        activityId,
                        useAdaptiveCard,
                        status,
                        summaries.ToString(),
                        chatResponse.Citations?.ToList(),
                        turnContext,
                        cancellationToken);
                }
                botResponseInteraction.TimeToLastResponse = (int)(DateTime.UtcNow - started).TotalMilliseconds;
                DiagnosticService.RecordTimeToLastResponse(botResponseInteraction.TimeToLastResponse);

                // write the generated message
                botResponseInteraction.Message = botResponseInteraction.State != States.EMPTY
                    ? summaries.ToString()
                    : null;

                // complete the generation
                var completeGenerationRequest = new CompleteGenerationRequest(activityId);
                var completeGenerationResponse = await httpClient.PutAsJsonAsync(
                    $"{this.config.MEMORY_URL}/api/users/{userId}/conversations/current/turns",
                    completeGenerationRequest,
                    cancellationToken);
                startGenerationResponse.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Locked)
            {
                await Dispatch(
                    activityId,
                    false,
                    null,
                    "The assistant is already generating a response for you, please wait for that to complete or stop the generation.",
                    null,
                    turnContext,
                    cancellationToken);
                return;
            }
            catch (Exception)
            {
                // botResponseInteraction.State = States.FAILED;
                // botResponseInteraction.Message = summaries.ToString();

                // TODO: add polly
                var completeGenerationRequest = new CompleteGenerationRequest(activityId);
                var completeGenerationResponse = await httpClient.PutAsJsonAsync(
                    $"{this.config.MEMORY_URL}/api/users/{userId}/conversations/current/turns",
                    completeGenerationRequest,
                    cancellationToken);
                startGenerationResponse.EnsureSuccessStatusCode();

                throw;
            }
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
