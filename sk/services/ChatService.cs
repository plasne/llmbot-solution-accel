using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using DistributedChat;
using static DistributedChat.ChatService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using Shared.Models.Memory;
using Newtonsoft.Json;

namespace Inference;

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

    private static async Task Flush(Buffer buffer, IServerStreamWriter<ChatResponse> responseStream)
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
    }

    public override async Task Chat(
        ChatRequest request,
        IServerStreamWriter<ChatResponse> responseStream,
        ServerCallContext context)
    {
        // get current conversation
        using var httpClient = this.httpClientFactory.CreateClient("retry");
        var res = await httpClient.GetAsync(
            $"{this.config.MEMORY_URL}/api/users/{request.UserId}/conversations/:last",
            context.CancellationToken);
        var responseContent = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            throw new Exception($"failed to get conversation for user {request.UserId}: {responseContent}");
        }
        var conversation = JsonConvert.DeserializeObject<Conversation>(responseContent);
        if (conversation?.Turns is null || !conversation.Turns.Any())
        {
            throw new Exception($"no turns were found for user {request.UserId}");
        }

        // build grounding data
        var turns = conversation.Turns.ToList();
        var userQuery = turns.LastOrDefault();
        if (userQuery is null || userQuery.Role != Roles.USER || string.IsNullOrEmpty(userQuery.Msg))
        {
            throw new Exception($"the last turn must be a query from the user.");
        }
        turns.Remove(userQuery);
        var groundingData = new GroundingData
        {
            UserQuery = userQuery.Msg,
            History = turns,
        };

        // create scope, context, and workflow
        using var scope = this.serviceProvider.CreateScope();
        var workflowContext = scope.ServiceProvider.GetRequiredService<IWorkflowContext>();
        var workflow = scope.ServiceProvider.GetRequiredService<Workflow>();

        // add stream event
        // NOTE: we should always end on a status change or it isn't flushed
        var buffer = new Buffer();
        workflowContext.OnStream += async (status, message, intent, citations, promptTokens, completionTokens) =>
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
                await Flush(buffer, responseStream);
            }

            // send if the buffer is full
            if (buffer.Message.Length >= request.MinCharsToStream)
            {
                await Flush(buffer, responseStream);
            }
        };

        // execute the workflow
        await workflow.Execute(groundingData, context.CancellationToken);
    }
}