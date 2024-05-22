using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Newtonsoft.Json;
using System.IO;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading;
using SharpToken;
using System;
using Shared;

namespace Inference;

public class DetermineIntent(
    IConfig config,
    IWorkflowContext context,
    Kernel kernel,
    IMemory memory,
    ILogger<DetermineIntent> logger)
    : BaseStep<WorkflowRequest, DeterminedIntent>(logger)
{
    private readonly IConfig config = config;
    private readonly IWorkflowContext context = context;
    private readonly Kernel kernel = kernel;
    private readonly IMemory memory = memory;
    private readonly ILogger<DetermineIntent> logger = logger;

    public override string Name => "DetermineIntent";

    public override async Task<DeterminedIntent> ExecuteInternal(
        WorkflowRequest input,
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

        // add prompt token count reporting using sharpToken
        kernel.PromptFilters.Add(new PromptTokenCountFilter(this.config.LLM_MODEL_NAME, count =>
        {
            this.Usage.PromptTokenCount = count;
            DiagnosticService.RecordPromptTokenCount(count, this.config.LLM_MODEL_NAME);
        }));

        // build the history
        ChatHistory history = input.History?.ToChatHistory() ?? [];

        // execute
        var startTime = DateTime.UtcNow;
        var response = await this.kernel.InvokeAsync(
            func,
            new()
            {
                { "history", history },
                { "query", input.UserQuery },
            },
            cancellationToken);
        var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

        // record completion token count using sharpToken
        var encoding = GptEncoding.GetEncoding(this.config.LLM_ENCODING_MODEL);
        this.Usage.CompletionTokenCount = encoding.CountTokens(response.ToString());
        DiagnosticService.RecordCompletionTokenCount(this.Usage.CompletionTokenCount, this.config.LLM_MODEL_NAME);

        // record tokens per second
        var tokensPerSecond = this.Usage.CompletionTokenCount / elapsedSeconds;
        DiagnosticService.RecordTokensPerSecond(tokensPerSecond, this.config.LLM_MODEL_NAME);

        // deserialize the response
        // NOTE: this could maybe be a retry (transient fault)
        var intent = JsonConvert.DeserializeObject<DeterminedIntent>(response.ToString())
            ?? throw new HttpException(500, "Intent could not be deserialized.");

        // if in debug mode, log the intent
        this.logger.LogDebug(response.ToString());

        // send token counts
        await this.context.Stream(promptTokens: this.Usage.PromptTokenCount, completionTokens: this.Usage.CompletionTokenCount);

        // record to context
        return intent;
    }
}