using System.Threading.Tasks;
using BoardGameChat;
using Grpc.Core;
using Microsoft.SemanticKernel.ChatCompletion;
using static BoardGameChat.BoardGameChats;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class BoardGameChatService(
    Kernel kernel,
    SearchService searchService,
    ILogger<BoardGameChatService> logger)
    : BoardGameChatsBase
{
    private readonly Kernel kernel = kernel;
    private readonly SearchService searchService = searchService;
    private readonly ILogger<BoardGameChatService> logger = logger;

    public override async Task Chat(
        ChatRequest request,
        IServerStreamWriter<ChatResponse> responseStream,
        ServerCallContext context)
    {
        var history = new ChatHistory();
        history.AddUserMessage(request.Usr);
        await foreach (var chunk in this.GetBoardGameRules(history))
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var response = new ChatResponse
            {
                Msg = chunk.ToString()
            };
            await responseStream.WriteAsync(response);
        }
    }

    private async IAsyncEnumerable<StreamingKernelContent> GetBoardGameRules(ChatHistory history)
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
        var intentResponse = await this.kernel.InvokeAsync(
            getIntent,
            new()
            {
                { "history", history }
            }
        );
        var intent = JsonConvert.DeserializeObject<Intent>(intentResponse.ToString());
        this.logger.LogDebug("intent: {i}", JsonConvert.SerializeObject(intent));

        // run the queries
        var contextChunks = new List<string>();
        if (intent?.SearchQueries is not null)
        {
            foreach (var query in intent.SearchQueries)
            {
                await foreach (var result in searchService.SearchAsync(query))
                {
                    int index = contextChunks.Count;
                    var chunk = "[doc" + index + "]\nTitle:" + result.Title + "\n" + result.Chunk + "\n[/doc" + index + "]";
                    this.logger.LogDebug(chunk);
                    contextChunks.Add(chunk);
                }
            }
        }
        var context = string.Join("\n", contextChunks);
        this.logger.LogDebug("contextChunks: {i}", contextChunks.Count);

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
        var chatResponses = this.kernel.InvokeStreamingAsync(
            continueChat,
            new()
            {
                { "history", history },
                { "query", intent?.Query ?? history.Last().ToString() },
                { "context", context }
            }
        );

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