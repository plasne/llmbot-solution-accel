using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Newtonsoft.Json;
using System.IO;
using Microsoft.SemanticKernel.ChatCompletion;
using System;

public class DetermineIntent(
    IContext context,
    Kernel kernel,
    IMemory memory,
    ILogger<DetermineIntent> logger)
    : BaseStep<GroundingData, Intent>(logger)
{
    private readonly IContext context = context;
    private readonly Kernel kernel = kernel;
    private readonly IMemory memory = memory;
    private readonly ILogger<DetermineIntent> logger = logger;

    public override string Name => "DetermineIntent";

    public override async Task<Intent> Execute(GroundingData input)
    {
        // validate input
        if (string.IsNullOrEmpty(input?.UserQuery))
        {
            throw new Exception("UserQuery is required.");
        }

        // set the status
        await this.context.SetStatus("Determining intent...");

        // get or set the prompt template
        string template = await this.memory.GetOrSet("prompt:intent", null, () =>
        {
            return File.ReadAllTextAsync("prompts/intent.txt");
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
        ChatHistory history = input.History?.ToChatHistory() ?? [];

        // execute
        var response = await this.kernel.InvokeAsync(
            func,
            new()
            {
                { "history", history },
                { "query", input.UserQuery },
            }
        );

        // record to context
        return JsonConvert.DeserializeObject<Intent>(response.ToString());
    }
}