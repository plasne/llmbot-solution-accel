using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using DistributedChat;
using static DistributedChat.ChatService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;

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
        var flush = new Func<Task>(async () =>
        {
            var response = new ChatResponse
            {
                Status = status,
                Msg = buffer.ToString(),
            };
            buffer.Clear();
            await responseStream.WriteAsync(response);
        });

        // add stream event
        context.OnStream += async (proposedStatus, message) =>
        {
            buffer.Append(message);

            // always flush if status changes
            if (!string.IsNullOrEmpty(proposedStatus) && proposedStatus != status)
            {
                status = proposedStatus;
                await flush();
            }

            // send if the buffer is full
            if (buffer.Length >= request.MinCharsToStream)
            {
                await flush();
            }
        };

        // execute the workflow
        await workflow.Execute(groundingData, serverCallContext.CancellationToken);

        // flush any remaining content
        if (buffer.Length > 0)
        {
            await flush();
        }
    }
}