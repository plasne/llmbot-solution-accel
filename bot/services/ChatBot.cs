using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using DistributedChat;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Models.Memory;
using Citation = Shared.Models.Memory.Citation;

namespace Bot;

public class ChatBot(
    IConfig config,
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider,
    ICardProvider cardProvider,
    IHttpClientFactory httpClientFactory,
    BotChannel channel,
    StopUserMessageMemory stopUserMessageMemory,
    ILogger<ChatBot> logger)
    : ActivityHandler
{
    private readonly IConfig config = config;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ICardProvider cardProvider = cardProvider;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly BotChannel channel = channel;
    private readonly StopUserMessageMemory stopUserMessageMemory = stopUserMessageMemory;
    private readonly ILogger<ChatBot> logger = logger;
    private readonly DateTime started = httpContextAccessor.HttpContext is not null
        && httpContextAccessor.HttpContext.Items.TryGetValue(StartTimeKey, out var requestStartTime)
        && requestStartTime is DateTime requestStart
        ? requestStart
        : DateTime.UtcNow;
    private readonly Queue<string> requests = new();

    private bool previousTopicChange = false;

    public const string StartTimeKey = "http-request-start-time";

    private bool IsAuthorized(Func<string> getTenantId)
    {
        // if no tenants are specified, then all are valid
        if (config.VALID_TENANTS.Length == 0)
        {
            logger.LogDebug("no tenants are specified, all are authorized to use the bot.");
            return true;
        }

        // ensure the tenant is whitelisted
        try
        {
            string tenantId = getTenantId();
            if (config.VALID_TENANTS.Contains(tenantId))
            {
                logger.LogDebug("the tenant {t} is authorized to use the bot.", tenantId);
                return true;
            }

            logger.LogWarning("the tenant {t} is not authorized to use the bot.", tenantId);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "the tenant could not be determined from the activity...");
            return false;
        }
    }

    private async Task<string> Dispatch(
        string? activityId,
        bool useAdaptiveCard,
        string? status,
        string reply,
        List<DistributedChat.Citation>? citations,
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        // build the activity
        IMessageActivity activity;
        if (useAdaptiveCard)
        {
            // load the template
            var template = await cardProvider.GetTemplate("response");
            var isGenerated = status == config.FINAL_STATUS;
            Func<string, string> Expand = (msg) =>
            {
                var data = new
                {
                    activityId,
                    status,
                    reply = msg,
                    citations = citations?
                        .SelectMany(x => x.Uris.Select(uri => new Citation { Ref = x.Id, Uri = uri }))
                        .ToList() ?? [],
                    showFeedback = isGenerated,
                    showStop = !isGenerated,
                    showDelete = isGenerated,
                    showUpVoteSelected = false,
                    showDownVoteSelected = false
                };
                return template.Expand(data);
            };

            // build the adaptive card
            string content = Expand(reply);
            if (content.Length > this.config.MAX_PAYLOAD_SIZE)
            {
                var truncated = reply.Truncate(content.Length, this.config.MAX_PAYLOAD_SIZE);
                content = Expand(truncated);
            }

            // add adaptive card as attachment
            var attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(content),
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
        var commands = serviceProvider.GetServices<ICommands>();
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
        if (helpCommand is not null)
        {
            await helpCommand.ShowHelp(turnContext, cancellationToken);
        }
        return true;
    }

    private async Task<Guid> StartGenerationAsync(HttpClient httpClient, string userId, StartGenerationRequest req, CancellationToken cancellationToken)
    {
        var res = await httpClient.PostAsync(
            $"{config.MEMORY_URL}/api/users/{userId}/conversations/:last/turns",
            req.ToJsonContent(),
            cancellationToken);
        var content = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            logger.LogError("the attempt to start generation resulted in HTTP {status} - {content}.", res.StatusCode, content);
        }
        res.EnsureSuccessStatusCode();
        var payload = JsonConvert.DeserializeObject<StartGenerationResponse>(content)
            ?? throw new Exception("no conversation ID was received from the memory service.");
        return payload.ConversationId;
    }

    private async Task CompleteGenerationAsync(HttpClient httpClient, string userId, CompleteGenerationRequest req, List<DistributedChat.Citation>? citations, CancellationToken cancellationToken)
    {
        req.Citations = citations?
            .SelectMany(x => x.Uris.Select(uri => new Citation { Ref = x.Id, Uri = uri }))
            .ToList() ?? [];
        var res = await httpClient.PutAsync(
            $"{config.MEMORY_URL}/api/users/{userId}/conversations/:last/turns/:last",
            req.ToJsonContent(),
            cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("the attempt to complete generation resulted in HTTP {status} - {content}.", res.StatusCode, content);
        }
        res.EnsureSuccessStatusCode();
    }

    private async Task<string> HandleResponse(
        ITurnContext<IMessageActivity> turnContext,
        string activityId,
        ChatResponse chatResponse,
        List<DistributedChat.Citation> citations,
        StringBuilder summaries,
        CompleteGenerationRequest completeRequest,
        CancellationToken cancellationToken)
    {
        // the LLM may have determined the user's intent is something other than what the LLM can provide
        var status = chatResponse.Status;
        var useAdaptiveCard = true;
        switch (chatResponse.Intent)
        {
            case Intent.Unknown:
                break;
            case Intent.InDomain:
                completeRequest.Intent = Intents.IN_DOMAIN;
                break;
            case Intent.Greeting:
                completeRequest.Intent = Intents.GREETING;
                status = config.FINAL_STATUS;
                summaries.ResetTo("Hello and welcome! If you aren't sure what I can do type `/help`.");
                break;
            case Intent.OutOfDomain:
                completeRequest.Intent = Intents.OUT_OF_DOMAIN;
                break;
            case Intent.Goodbye:
                completeRequest.Intent = Intents.GOODBYE;
                status = config.FINAL_STATUS;
                summaries.ResetTo("Goodbye!");
                break;
            case Intent.TopicChange:
                completeRequest.ConversationId = Guid.NewGuid();
                completeRequest.Intent = Intents.TOPIC_CHANGE;
                completeRequest.State = States.EMPTY;
                previousTopicChange = true;
                if (!string.IsNullOrEmpty(chatResponse.Msg))
                {
                    requests.Enqueue(chatResponse.Msg);
                }
                status = config.FINAL_STATUS;
                useAdaptiveCard = false;
                summaries.ResetTo("Let's start a new conversation.");
                break;
        }

        // dispatch the response
        activityId = await this.Dispatch(
            activityId,
            useAdaptiveCard,
            status,
            summaries.ToString(),
            citations,
            turnContext,
            cancellationToken);

        return activityId;
    }

    private async Task ProcessNextRequest(ITurnContext<IMessageActivity> turnContext, string userId, CancellationToken cancellationToken)
    {
        // send the "connection" message
        var activityId = await this.Dispatch(activityId: null, useAdaptiveCard: true, status: "Connecting to assistant...", reply: string.Empty, citations: null, turnContext, cancellationToken);

        // create the completed request (can be modifided as the conversation progresses)
        var completeRequest = new CompleteGenerationRequest
        {
            ConversationId = Guid.Empty,
            ActivityId = activityId,
            State = States.UNMODIFIED,
            Intent = Intents.UNKNOWN
        };

        // process next message
        using var httpClient = httpClientFactory.CreateClient("retry");
        StringBuilder summaries = new();
        var citations = new Dictionary<string, DistributedChat.Citation>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            // derive a cancellation token here        
            stopUserMessageMemory.TryAdd(activityId, cts);

            // start the generation
            var request = requests.Dequeue();
            completeRequest.ConversationId = await this.StartGenerationAsync(
                httpClient,
                userId,
                new StartGenerationRequest { RequestActivityId = turnContext.Activity.Id, Query = request, ResponseActivityId = activityId },
                cts.Token);

            // create the request
            var chatRequest = new ChatRequest
            {
                UserId = userId,
                MinCharsToStream = config.CHARACTERS_PER_UPDATE,
                PreviousTopicChange = previousTopicChange,
            };

            // send the request
            using var streamingCall = channel.Client.Chat(chatRequest, cancellationToken: cts.Token);

            // start receiving the async responses
            await foreach (var chatResponse in streamingCall.ResponseStream.ReadAllAsync(cts.Token))
            {
                // append the summary with any message
                if (!string.IsNullOrEmpty(chatResponse.Msg))
                {
                    if (completeRequest.TimeToFirstResponse == 0)
                    {
                        completeRequest.TimeToFirstResponse = (int)(DateTime.UtcNow - started).TotalMilliseconds;
                        DiagnosticService.RecordTimeToFirstResponse(completeRequest.TimeToFirstResponse);
                    }
                    summaries.Append(chatResponse.Msg);
                }

                // add any citations that were found in the response
                if (chatResponse.Citations is not null)
                {
                    foreach (var citation in chatResponse.Citations)
                    {
                        citations.TryAdd(citation.Id, citation);
                    }
                }

                // add any token counts
                completeRequest.PromptTokenCount += chatResponse.PromptTokens;
                completeRequest.CompletionTokenCount += chatResponse.CompletionTokens;
                completeRequest.EmbeddingTokenCount += chatResponse.EmbeddingTokens;

                // handle the response
                activityId = await this.HandleResponse(
                    turnContext,
                    activityId,
                    chatResponse,
                    citations.Values.ToList(),
                    summaries,
                    completeRequest,
                    cts.Token);
            }
            completeRequest.TimeToLastResponse = (int)(DateTime.UtcNow - started).TotalMilliseconds;
            DiagnosticService.RecordTimeToLastResponse(completeRequest.TimeToLastResponse);
            if (completeRequest.CompletionTokenCount > 0)
            {
                DiagnosticService.RecordTimePerOutputToken((completeRequest.TimeToLastResponse - completeRequest.TimeToFirstResponse) / completeRequest.CompletionTokenCount);
            }

            // if it is not supposed to be empty, interrorgate the message
            if (completeRequest.State != States.EMPTY)
            {
                completeRequest.Message = summaries.ToString();
                if (string.IsNullOrEmpty(completeRequest.Message))
                {
                    completeRequest.Message = "I'm sorry, I can't help with that. If you aren't sure what I can do type `/help`.";
                    await this.Dispatch(
                        activityId: activityId,
                        useAdaptiveCard: true,
                        status: this.config.FINAL_STATUS,
                        reply: completeRequest.Message,
                        citations: null,
                        turnContext,
                        cts.Token);
                }
            }

            // complete the conversation
            await this.CompleteGenerationAsync(httpClient, userId, completeRequest, citations.Values.ToList(), cts.Token);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Locked)
        {
            await this.Dispatch(
                activityId: activityId,
                useAdaptiveCard: false,
                status: null,
                reply: "The assistant is already generating a response for you, please wait for that to complete or stop the generation.",
                citations: null,
                turnContext,
                cancellationToken);
            return;
        }
        catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
        {
            completeRequest.Message = summaries.ToString();
            completeRequest.State = States.STOPPED;

            await this.Dispatch(
                  activityId: activityId,
                  useAdaptiveCard: !string.IsNullOrEmpty(completeRequest.Message),
                  status: string.IsNullOrEmpty(completeRequest.Message) ? null : "Generated.",
                  reply: string.IsNullOrEmpty(completeRequest.Message) ? "The assistant has stopped generating a response based on your request." : completeRequest.Message,
                  citations: string.IsNullOrEmpty(completeRequest.Message) ? null : citations.Select(x => x.Value).ToList(),
                  turnContext,
                  cancellationToken);
            await this.CompleteGenerationAsync(httpClient, userId, completeRequest, citations.Values.ToList(), cancellationToken);
        }
        catch (Exception)
        {
            completeRequest.State = States.FAILED;
            completeRequest.Message = summaries.ToString();
            await this.CompleteGenerationAsync(httpClient, userId, completeRequest, citations.Values.ToList(), cancellationToken);
            throw;
        }
        finally
        {
            stopUserMessageMemory.TryRemove(activityId, shouldCancel: false);
        }
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        // verify authorization
        var userId = this.ValidateAndGetUserId(turnContext.Activity);
        logger.LogDebug("OnMessageActivityAsync received a message from user {u}.", userId);

        // see if this is a command
        if (await this.TryAsCommand(turnContext, cancellationToken))
        {
            return;
        }

        // get the text
        var text = turnContext.Activity.Text;
        if (string.IsNullOrEmpty(text))
        {
            throw new Exception("no text was found in the activity from the user.");
        }

        // enqueue the request, process it, and then process any request that come out of that
        requests.Enqueue(text);
        while (requests.Count > 0)
        {
            await this.ProcessNextRequest(turnContext, userId, cancellationToken);
        }
    }

    protected override async Task OnMessageUpdateActivityAsync(ITurnContext<IMessageUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var userId = this.ValidateAndGetUserId(turnContext.Activity);
        logger.LogDebug("OnMessageUpdateActivityAsync received a message from user {u}.", userId);

        UserMessageRequest request = new()
        {
            ActivityId = turnContext.Activity.Id,
            Message = turnContext.Activity.Text,
        };
        using var httpClient = this.httpClientFactory.CreateClient("retry");
        var res = await httpClient.PutAsync(
            $"{config.MEMORY_URL}/api/users/{userId}/activities/{turnContext.Activity.Id}",
            request.ToJsonContent(),
            cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"the attempt to update message resulted in HTTP {res.StatusCode} - {content}.");
        }
    }

    protected override async Task OnMessageDeleteActivityAsync(ITurnContext<IMessageDeleteActivity> turnContext, CancellationToken cancellationToken)
    {
        var userId = this.ValidateAndGetUserId(turnContext.Activity);
        logger.LogDebug("OnMessageDeleteActivityAsync received a message from user {u}.", userId);

        using var httpClient = httpClientFactory.CreateClient("retry");
        var res = await httpClient.DeleteAsync(
            $"{config.MEMORY_URL}/api/users/{userId}/activities/{turnContext.Activity.Id.ToBase64()}",
            cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"the attempt to delete message resulted in HTTP {res.StatusCode} - {content}.");
        }
    }

    private string ValidateAndGetUserId(IActivity activity)
    {
        // verify authorization
        if (!this.IsAuthorized(() => activity.ChannelData["tenant"]["id"].ToString()))
        {
            throw new Exception("the activity was not authorized to use this bot.");
        }

        var userId = activity.From.AadObjectId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("no user identity was found in the activity from the user.");
        }

        return userId;
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
