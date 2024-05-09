using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using DistributedChat;
using static DistributedChat.ChatService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Shared.Models.Memory;
using System.Net.Http.Json;

public class ChatService(IConfig config, IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
    : ChatServiceBase
{
    private readonly IConfig config = config;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

    private class Buffer
    {
        public string? Status { get; set; }
        public StringBuilder Message { get; } = new();
        public Intent Intent { get; set; }
        public List<Citation> Citations { get; } = [];
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }

    public override async Task Chat(
        ChatRequest request,
        IServerStreamWriter<ChatResponse> responseStream,
        ServerCallContext serverCallContext)
    {
        // get current conversation
        using var httpClient = this.httpClientFactory.CreateClient("retry");
        var res = await httpClient.GetAsync(
            $"{this.config.MEMORY_URL}/api/users/{request.UserId}/conversations/current",
            serverCallContext.CancellationToken);
        res.EnsureSuccessStatusCode();
        var conversation = await res.Content.ReadFromJsonAsync<Conversation>();
        if (conversation?.Turns is null || !conversation.Turns.Any())
        {
            throw new Exception($"no conversation was found for user {request.UserId}");
        }

        // build grounding data
        var turns = conversation.Turns.ToList();
        var userQuery = turns.Last();
        turns.Remove(userQuery);
        var groundingData = new GroundingData
        {
            UserQuery = userQuery.Msg,
            History = turns,
        };

        // create scope, context, and workflow
        using var scope = this.serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();
        var workflow = scope.ServiceProvider.GetRequiredService<Workflow>();

        var logger = this.serviceProvider.GetRequiredService<ILogger<ChatService>>();

        // setup buffering
        var buffer = new Buffer();
        var flush = new Func<Task>(async () =>
        {
            // build the response
            var response = new ChatResponse();
            if (buffer.Status is not null)
            {
                response.Status = buffer.Status;
            }
            if (buffer.Message.Length > 0)
            {
                response.Msg = buffer.Message.ToString();
                buffer.Message.Clear();
            }
            if (buffer.Intent != Intent.Unset)
            {
                response.Intent = buffer.Intent;
                buffer.Intent = Intent.Unset;
            }
            if (buffer.Citations.Count > 0)
            {
                response.Citations.AddRange(buffer.Citations);
                buffer.Citations.Clear();
            }
            if (buffer.PromptTokens > 0)
            {
                response.PromptTokens = buffer.PromptTokens;
                buffer.PromptTokens = 0;
            }
            if (buffer.CompletionTokens > 0)
            {
                response.CompletionTokens = buffer.CompletionTokens;
                buffer.CompletionTokens = 0;
            }

            // send the message
            await responseStream.WriteAsync(response);
        });

        // add stream event
        // NOTE: we should always end on a status change or it isn't flushed
        context.OnStream += async (status, message, intent, citations, promptTokens, completionTokens) =>
        {
            // add to the buffer
            buffer.Message.Append(message);
            if (intent != Intent.Unset)
            {
                buffer.Intent = intent;
            }
            if (citations is not null)
            {
                buffer.Citations.AddRange(citations);
            }
            buffer.PromptTokens += promptTokens;
            buffer.CompletionTokens += completionTokens;

            // always flush if status change
            if (!string.IsNullOrEmpty(status) && status != buffer.Status)
            {
                buffer.Status = status;
                await flush();
            }

            // send if the buffer is full
            if (buffer.Message.Length >= request.MinCharsToStream)
            {
                await flush();
            }
        };

        // execute the workflow
        await workflow.Execute(groundingData, serverCallContext.CancellationToken);
    }
}