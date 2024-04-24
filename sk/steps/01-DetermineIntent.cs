using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Newtonsoft.Json;
using System.IO;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading;

public class DetermineIntent(
    IContext context,
    Kernel kernel,
    IMemory memory,
    ILogger<DetermineIntent> logger)
    : BaseStep<GroundingData, DeterminedIntent>(logger)
{
    private readonly IContext context = context;
    private readonly Kernel kernel = kernel;
    private readonly IMemory memory = memory;

    public override string Name => "DetermineIntent";

    public override async Task<DeterminedIntent> ExecuteInternal(
        GroundingData input,
        CancellationToken cancellationToken = default)
    {
        // validate input
        if (string.IsNullOrEmpty(input?.UserQuery))
        {
            throw new HttpException(400, "user_query is required.");
        }

        // set the status
        await this.context.Stream("Determining intent...");

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
            },
            cancellationToken);

        // deserialize the response
        // NOTE: this could maybe be a retry (transient fault)
        var intent = JsonConvert.DeserializeObject<DeterminedIntent>(response.ToString())
            ?? throw new HttpException(500, "Intent could not be deserialized.");

        // record to context
        return intent;
    }
}