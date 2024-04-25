using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Newtonsoft.Json;
using System.IO;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading;
using Azure.AI.OpenAI;
using SharpToken;
using System;

public class DetermineIntent(
    IConfig config,
    IContext context,
    Kernel kernel,
    IMemory memory,
    ILogger<DetermineIntent> logger)
    : BaseStep<GroundingData, DeterminedIntent>(logger)
{
    private readonly IConfig config = config;
    private readonly IContext context = context;
    private readonly Kernel kernel = kernel;
    private readonly IMemory memory = memory;
    private readonly ILogger<DetermineIntent> logger = logger;

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

        // add prompt token count reporting using sharpToken
        kernel.PromptFilters.Add(new PromptTokenCountFilter(this.config.LLM_MODEL_NAME, this.GetType().Name, this.logger));

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
        var completionTokenCount = 0;
        if (response.Metadata is not null && response.Metadata.TryGetValue("Usage", out var usageOut) && usageOut is CompletionsUsage usage)
        {
            var encoding = GptEncoding.GetEncoding(this.config.LLM_ENCODING_MODEL);
            completionTokenCount = encoding.CountTokens(response.ToString());
            if (completionTokenCount != usage.CompletionTokens)
            {
                this.LogWarning("Completion token count mismatch: {completionTokenCount} != {usage.CompletionTokens}");
            }
            DiagnosticService.RecordCompletionTokenCount(completionTokenCount, this.config.LLM_MODEL_NAME, this.GetType().Name);
        }

        // record tokens per second
        if (completionTokenCount > 0)
        {
            var tokensPerSecond = completionTokenCount / elapsedSeconds;
            DiagnosticService.RecordTokensPerSecond(tokensPerSecond, this.config.LLM_MODEL_NAME, this.GetType().Name);
        }

        // deserialize the response
        // NOTE: this could maybe be a retry (transient fault)
        var intent = JsonConvert.DeserializeObject<DeterminedIntent>(response.ToString())
            ?? throw new HttpException(500, "Intent could not be deserialized.");

        // record to context
        return intent;
    }
}