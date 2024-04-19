using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using DistributedChat;
using static DistributedChat.ChatService;
using Microsoft.Extensions.DependencyInjection;
using System;

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

        // add status event
        context!.OnStatus += async (status) =>
        {
            var response = new ChatResponse { Status = status };
            await responseStream.WriteAsync(response);
        };

        // add stream event
        context!.OnStream += async (fragment) =>
        {
            var response = new ChatResponse
            {
                Status = "Generating answer...",
                Msg = fragment,
            };
            await responseStream.WriteAsync(response);
        };

        // execute the workflow
        await workflow.Execute(groundingData); // serverCallContext.CancellationToken.IsCancellationRequested
    }
}