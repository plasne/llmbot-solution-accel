using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

public class GenerateAnswer(
    IContext context,
    Kernel kernel,
    IMemory memory,
    ILogger<GenerateAnswer> logger)
    : BaseStep<IntentAndData, string>(logger)
{
    private readonly IContext context = context;
    private readonly Kernel kernel = kernel;
    private readonly IMemory memory = memory;

    public override string Name => "GenerateAnswer";

    public override async Task<string> ExecuteInternal(IntentAndData input)
    {
        // validate input
        var query = input.Intent?.Query ?? input.Data?.UserQuery;
        if (string.IsNullOrEmpty(query))
        {
            throw new HttpException(400, "A query is required.");
        }

        // get or set the prompt template
        string template = await this.memory.GetOrSet("prompt:chat", null, () =>
        {
            return File.ReadAllTextAsync("prompts/chat.txt");
        });

        // build the function
        var func = this.kernel.CreateFunctionFromPrompt(
            new()
            {
                Template = template,
                TemplateFormat = "handlebars"
            },
            new HandlebarsPromptTemplateFactory()
        );

        // build the history
        ChatHistory history = input.Data?.History?.ToChatHistory() ?? [];

        // get the responses
        var response = this.kernel.InvokeStreamingAsync(
            func,
            new()
            {
                { "history", history },
                { "query", query },
                { "context", input.Data?.Content }
            }
        );

        // stream each fragment
        var answer = new StringBuilder();
        await foreach (var fragment in response)
        {
            answer.Append(fragment.ToString());
            await this.context.Stream(fragment.ToString());
        }

        return answer.ToString();
    }
}