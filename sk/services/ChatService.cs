using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using DistributedChat;
using static DistributedChat.ChatService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Collections.Generic;

public class ChatService(IServiceProvider serviceProvider)
    : ChatServiceBase
{
    private readonly IServiceProvider serviceProvider = serviceProvider;

    private class Buffer
    {
        public string? Status { get; set; }
        public StringBuilder Text { get; } = new();
        public List<Citation> Citations { get; } = [];
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }

    public override async Task Chat(
        ChatRequest request,
        IServerStreamWriter<ChatResponse> responseStream,
        ServerCallContext serverCallContext)
    {
        // build grounding data
        var turns = request.Turns?.ToList();
        var userQuery = turns?.LastOrDefault();
        turns?.Remove(userQuery);
        var groundingData = new GroundingData
        {
            UserQuery = userQuery?.Msg,
            History = turns,
        };

        // create scope, context, and workflow
        using var scope = this.serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();
        var workflow = scope.ServiceProvider.GetRequiredService<Workflow>();

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
            if (buffer.Text.Length > 0)
            {
                response.Msg = buffer.Text.ToString();
                buffer.Text.Clear();
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
        context.OnStream += async (status, message, citations, promptTokens, completionTokens) =>
        {
            // buffer
            buffer.Text.Append(message);
            buffer.PromptTokens += promptTokens;
            buffer.CompletionTokens += completionTokens;

            // always flush if status change
            if (!string.IsNullOrEmpty(status) && status != buffer.Status)
            {
                buffer.Status = status;
                await flush();
            }

            // send if the buffer is full
            if (buffer.Text.Length >= request.MinCharsToStream)
            {
                await flush();
            }
        };

        // add stream intent event
        context.OnTerminate += async (intent, message) =>
        {
            // build the response
            var response = new ChatResponse
            {
                Intent = intent,
                Msg = message ?? "",
            };

            // send the message
            await responseStream.WriteAsync(response);
        };

        // execute the workflow
        await workflow.Execute(groundingData, serverCallContext.CancellationToken);
    }
}