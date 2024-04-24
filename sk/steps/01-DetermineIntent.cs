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
    IContext context,
    Kernel kernel,
    IMemory memory,
    ILogger<DetermineIntent> logger)
    : BaseStep<GroundingData, Intent>(logger)
{
    private readonly IContext context = context;
    private readonly Kernel kernel = kernel;
    private readonly IMemory memory = memory;

    public override string Name => "DetermineIntent";

    public override async Task<Intent> ExecuteInternal(
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
        var modelId = "";
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        if (chatCompletionService is not null && chatCompletionService.Attributes.TryGetValue("DeploymentName", out var deployModel) && deployModel is string model)
        {
            modelId = model;
            kernel.PromptFilters.Add(new PromptTokenCountFilter(modelId, this.GetType().Name));
        }

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
        var elapsedSeconds = (startTime - DateTime.UtcNow).TotalSeconds;

        // record completion token count using sharpToken
        var completionTokenCount = 0;
        if (!string.IsNullOrEmpty(modelId))
        {
            if (response.Metadata is not null && response.Metadata.TryGetValue("Usage", out var usageOut) && usageOut is CompletionsUsage usage)
            {
                var encoding = GptEncoding.GetEncodingForModel(modelId);
                completionTokenCount = encoding.CountTokens(response.ToString());
                if (completionTokenCount != usage.CompletionTokens)
                {
                    logger.LogWarning($"Completion token count mismatch: {completionTokenCount} != {usage.CompletionTokens}");
                }
                DiagnosticService.RecordCompletionTokenCount(completionTokenCount, this.GetType().Name);
            }
        }

        // record tokens per second
        if (completionTokenCount > 0)
        {
            var tokensPerSecond = completionTokenCount / elapsedSeconds;
            DiagnosticService.RecordTokensPerSecond(tokensPerSecond, this.GetType().Name);
        }

        // deserialize the response
        // NOTE: this could maybe be a retry (transient fault)
        var intent = JsonConvert.DeserializeObject<Intent>(response.ToString())
            ?? throw new HttpException(500, "Intent could not be deserialized.");

        // record to context
        return intent;
    }
}