using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DistributedChat;
using static DistributedChat.ChatService;
using Microsoft.Extensions.DependencyInjection;
using System;

public class ChatService(
    IServiceProvider serviceProvider,
    Workflow workflow)
    : ChatServiceBase
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly Workflow workflow = workflow;

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

        // create scope and context
        using var scope = this.serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetService<IContext>();

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
        await this.workflow.Execute(scope, groundingData); // serverCallContext.CancellationToken.IsCancellationRequested
    }
}