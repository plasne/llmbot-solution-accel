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
        string? status = null;
        var buffer = new StringBuilder();
        var flush = new Func<List<Citation>?, Task>(async (citations) =>
        {
            // build the response
            var response = new ChatResponse
            {
                Status = status,
                Msg = buffer.ToString(),
            };
            if (citations is not null)
            {
                response.Citations.AddRange(citations);
            }

            // clear the buffer
            buffer.Clear();

            // send the message
            await responseStream.WriteAsync(response);
        });

        // add stream event
        // NOTE: we should always end on a status change or it isn't flushed
        context.OnStream += async (proposedStatus, message, incomingCitations) =>
        {
            // append to buffers
            buffer.Append(message);

            // always flush if status changes or citations are added
            var statusChanged = !string.IsNullOrEmpty(proposedStatus) && proposedStatus != status;
            if (statusChanged || incomingCitations is not null)
            {
                status = proposedStatus;
                await flush(incomingCitations);
            }

            // send if the buffer is full
            if (buffer.Length >= request.MinCharsToStream)
            {
                await flush(incomingCitations);
            }
        };

        // execute the workflow
        await workflow.Execute(groundingData, serverCallContext.CancellationToken);
    }
}