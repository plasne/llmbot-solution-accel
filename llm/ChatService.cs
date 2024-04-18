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
using llm;

public class ChatService(
    Kernel kernel,
    SearchService searchService,
    ILogger<ChatService> logger)
    : ChatServiceBase
{
    private readonly Kernel kernel = kernel;
    private readonly SearchService searchService = searchService;
    private readonly ILogger<ChatService> logger = logger;

    public override async Task Chat(
        ChatRequest request,
        IServerStreamWriter<ChatResponse> responseStream,
        ServerCallContext context)
    {
        // build the history
        var history = new ChatHistory();
        foreach (var turn in request.Turns)
        {
            switch (turn.Role)
            {
                case "assistant":
                    history.AddAssistantMessage(turn.Msg);
                    break;
                case "user":
                    history.AddUserMessage(turn.Msg);
                    break;
            }
        }

        // continue the chat
        await foreach (var chunk in this.ContinueChat(history))
        {
            // break if requested
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            // send the response
            var response = new ChatResponse
            {
                Msg = chunk.ToString()
            };
            await responseStream.WriteAsync(response);
        }
    }

    private async IAsyncEnumerable<StreamingKernelContent> ContinueChat(ChatHistory history)
    {
        // build the getIntent function
        var intentTemplate = File.ReadAllText("prompts/intent.txt");
        var getIntent = this.kernel.CreateFunctionFromPrompt(
            new()
            {
                Template = intentTemplate,
                TemplateFormat = "handlebars"
            },
            new HandlebarsPromptTemplateFactory()
        );

        // get the intent
        Intent intent;
        using (var determineIntentActivity = DiagnosticService.Source.StartActivity("determineIntentStep"))
        {
            var intentResponse = await this.kernel.InvokeAsync(
            getIntent,
            new()
            {
                { "history", history }
            }
        );
            intent = JsonConvert.DeserializeObject<Intent>(intentResponse.ToString());
            this.logger.LogDebug("intent: {i}", JsonConvert.SerializeObject(intent));
        }

        // run the queries
        var contextChunks = new List<string>();
        if (intent?.SearchQueries is not null)
        {
            using (var retrievedDocumentsActivity = DiagnosticService.Source.StartActivity("retrievedDocumentsStep"))
            {
                foreach (var query in intent.SearchQueries)
                {
                    await foreach (var result in searchService.SearchAsync(query))
                    {
                        int index = contextChunks.Count;
                        var chunk = "[doc" + index + "]\nTitle:" + result.Title + "\n" + result.Chunk + "\n[/doc" + index + "]";
                        contextChunks.Add(chunk);
                    }
                }
            }
        }
        var context = string.Join("\n", contextChunks);
        this.logger.LogDebug("contextChunks: {i}", contextChunks.Count);

        IAsyncEnumerable<StreamingKernelContent> chatResponses;
        using (var retrievedDocumentsActivity = DiagnosticService.Source.StartActivity("replyStep"))
        {
            // build the continueChat function
            var chatTemplate = File.ReadAllText("prompts/chat.txt");
            var continueChat = this.kernel.CreateFunctionFromPrompt(
                new()
                {
                    Template = chatTemplate,
                    TemplateFormat = "handlebars"
                },
                new HandlebarsPromptTemplateFactory()
            );

            // get the responses
            chatResponses = this.kernel.InvokeStreamingAsync(
                continueChat,
                new()
                {
                { "history", history },
                { "query", intent?.Query ?? history.Last().ToString() },
                { "context", context }
                }
            );
        }

        // yield each response
        var totalResponse = new StringBuilder();
        await foreach (var chatResponse in chatResponses)
        {
            totalResponse.Append(chatResponse.ToString());
            yield return chatResponse;
        }

        // log the reference
        var matches = Regex.Matches(totalResponse.ToString(), @"\[doc\d+\]");
        foreach (Match match in matches)
        {
            string docNumber = match.Value.Replace("[doc", "").Replace("]", "");
            if (int.TryParse(docNumber, out int index) && index < contextChunks.Count)
            {
                this.logger.LogDebug("context: {c}", contextChunks[index]);
            }
        }
    }
}