﻿using System;
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

namespace Bot;

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
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ICardProvider cardProvider = cardProvider;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly BotChannel channel = channel;
    private readonly ILogger<ChatBot> logger = logger;
    private readonly DateTime started = httpContextAccessor.HttpContext is not null
        && httpContextAccessor.HttpContext.Items.TryGetValue(StartTimeKey, out var requestStartTime)
        && requestStartTime is DateTime requestStart
        ? requestStart
        : DateTime.UtcNow;
    private readonly Queue<string> requests = new();

    public const string StartTimeKey = "http-request-start-time";

    private bool IsAuthorized(ITurnContext<IMessageActivity> turnContext)
    {
        // if no tenants are specified, then all are valid
        if (this.config.VALID_TENANTS.Length == 0)
        {
            this.logger.LogDebug("no tenants are specified, all are authorized to use the bot.");
            return true;
        }

        // ensure the tenant is whitelisted
        try
        {
            string tenantId = turnContext.Activity.ChannelData["tenant"]["id"].ToString();
            if (this.config.VALID_TENANTS.Contains(tenantId))
            {
                this.logger.LogDebug("the tenant {t} is authorized to use the bot.", tenantId);
                return true;
            }

            this.logger.LogWarning("the tenant {t} is not authorized to use the bot.", tenantId);
            return false;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "the tenant could not be determined from the activity...");
            return false;
        }
    }

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
        if (citations is not null && citations.Count > 0)
        {
            StringBuilder sb = new(reply);
            sb.AppendLine("");
            sb.AppendLine("");
            sb.AppendLine(citations.Count > 1 ? "Sources" : "Source");

            foreach (var citation in citations)
            {
                foreach (var uri in citation.Uris)
                {
                    sb.AppendLine($"* [{citation.Id}] [{uri}]({uri})");
                }
            }
            reply = sb.ToString();
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

    private async Task<Guid> StartGenerationAsync(HttpClient httpClient, string userId, StartGenerationRequest req, CancellationToken cancellationToken)
    {
        var res = await httpClient.PostAsync(
            $"{this.config.MEMORY_URL}/api/users/{userId}/conversations/:last/turns",
            req.ToJsonContent(),
            cancellationToken);
        var content = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            this.logger.LogError("the attempt to start generation resulted in HTTP {status} - {content}.", res.StatusCode, content);
        }
        res.EnsureSuccessStatusCode();
        var payload = JsonConvert.DeserializeObject<StartGenerationResponse>(content)
            ?? throw new Exception("no conversation ID was received from the memory service.");
        return payload.ConversationId;
    }

    private async Task CompleteGenerationAsync(HttpClient httpClient, string userId, CompleteGenerationRequest req, CancellationToken cancellationToken)
    {
        var res = await httpClient.PutAsync(
            $"{this.config.MEMORY_URL}/api/users/{userId}/conversations/:last/turns/:last",
            req.ToJsonContent(),
            cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync(cancellationToken);
            this.logger.LogError("the attempt to complete generation resulted in HTTP {status} - {content}.", res.StatusCode, content);
        }
        res.EnsureSuccessStatusCode();
    }

    private async Task<string> HandleResponse(
        ITurnContext<IMessageActivity> turnContext,
        string activityId,
        ChatResponse chatResponse,
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
                status = this.config.FINAL_STATUS;
                summaries.ResetTo("Hello and welcome! If you aren't sure what I can do type `/help`.");
                break;
            case Intent.OutOfDomain:
                completeRequest.Intent = Intents.OUT_OF_DOMAIN;
                status = this.config.FINAL_STATUS;
                summaries.ResetTo("I'm sorry, I can't help with that. If you aren't sure what I can do type `/help`.");
                break;
            case Intent.Goodbye:
                completeRequest.Intent = Intents.GOODBYE;
                status = this.config.FINAL_STATUS;
                summaries.ResetTo("Goodbye!");
                break;
            case Intent.TopicChange:
                completeRequest.ConversationId = Guid.NewGuid();
                completeRequest.Intent = Intents.TOPIC_CHANGE;
                completeRequest.State = States.EMPTY;
                if (!string.IsNullOrEmpty(chatResponse.Msg))
                {
                    this.requests.Enqueue(chatResponse.Msg);
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

        return activityId;
    }

    private async Task ProcessNextRequest(ITurnContext<IMessageActivity> turnContext, string userId, CancellationToken cancellationToken)
    {
        // send the "connection" message
        var activityId = await Dispatch(activityId: null, useAdaptiveCard: true, status:  "Connecting to assistant...", reply: string.Empty, citations: null, turnContext, cancellationToken);

        // create the completed request (can be modifided as the conversation progresses)
        var completeRequest = new CompleteGenerationRequest
        {
            ConversationId = Guid.Empty,
            ActivityId = activityId,
            State = States.UNMODIFIED,
            Intent = Intents.UNKNOWN
        };

        // process next message
        using var httpClient = this.httpClientFactory.CreateClient("retry");
        StringBuilder summaries = new();
        var citations = new Dictionary<string, Citation>();
        try
        {
            // start the generation
            var request = this.requests.Dequeue();
            completeRequest.ConversationId = await StartGenerationAsync(
                httpClient,
                userId,
                new StartGenerationRequest { RequestActivityId = turnContext.Activity.Id, Query = request, ResponseActivityId = activityId },
                cancellationToken);

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
                // append the summary with any message
                if (!string.IsNullOrEmpty(chatResponse.Msg))
                {
                    if (completeRequest.TimeToFirstResponse == 0)
                    {
                        completeRequest.TimeToFirstResponse = (int)(DateTime.UtcNow - this.started).TotalMilliseconds;
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
                activityId = await this.HandleResponse(turnContext, activityId, chatResponse, summaries, completeRequest, cancellationToken);
            }
            completeRequest.TimeToLastResponse = (int)(DateTime.UtcNow - this.started).TotalMilliseconds;
            DiagnosticService.RecordTimeToLastResponse(completeRequest.TimeToLastResponse);
            DiagnosticService.RecordTimePerOutputToken((completeRequest.TimeToLastResponse - completeRequest.TimeToFirstResponse) / completeRequest.CompletionTokenCount);

            // write the generated message
            completeRequest.Message = completeRequest.State != States.EMPTY
                ? summaries.ToString()
                : null;
            if (string.IsNullOrEmpty(completeRequest.Message))
            {
                completeRequest.Message = "I'm sorry, I don't have a response for that. If you aren't sure what I can do type `/help`.";
                await Dispatch(
                    activityId: activityId,
                    useAdaptiveCard: true,
                    status: "Generated.",
                    reply: completeRequest.Message,
                    citations: null,
                    turnContext,
                    cancellationToken);
            }

            await this.CompleteGenerationAsync(httpClient, userId, completeRequest, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Locked)
        {
            await Dispatch(
                activityId: activityId,
                useAdaptiveCard: false,
                status: null,
                reply: "The assistant is already generating a response for you, please wait for that to complete or stop the generation.",
                citations: null,
                turnContext,
                cancellationToken);
            return;
        }
        catch (Exception)
        {
            completeRequest.State = States.FAILED;
            completeRequest.Message = summaries.ToString();
            await this.CompleteGenerationAsync(httpClient, userId, completeRequest, cancellationToken);
            throw;
        }
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        // verify authorization
        if (!this.IsAuthorized(turnContext))
        {
            throw new Exception("the activity was not authorized to use this bot.");
        }

        // get the user
        var userId = turnContext.Activity.From.AadObjectId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("no user identity was found in the activity from the user.");
        }
        this.logger.LogDebug("OnMessageActivityAsync received a message from user {u}.", userId);

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

        // enqueue the request, process it, and then process any request that come out of that
        this.requests.Enqueue(text);
        while (this.requests.Count > 0)
        {
            await this.ProcessNextRequest(turnContext, userId, cancellationToken);
        }
    }

    protected override Task OnMessageUpdateActivityAsync(ITurnContext<IMessageUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task OnMessageDeleteActivityAsync(ITurnContext<IMessageDeleteActivity> turnContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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
