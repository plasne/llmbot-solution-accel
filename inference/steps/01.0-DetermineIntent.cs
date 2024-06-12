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
using System.Collections.Generic;
using Azure.AI.OpenAI;

namespace Inference;

public class DetermineIntent(
    IConfig config,
    IWorkflowContext context,
    KernelFactory kernelFactory,
    IMemory memory,
    ILogger<DetermineIntent> logger)
    : BaseStep<WorkflowRequest, DeterminedIntent>(logger)
{
    private readonly IConfig config = config;
    private readonly IWorkflowContext context = context;
    private readonly KernelFactory kernelFactory = kernelFactory;
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
            return File.ReadAllTextAsync(this.config.INTENT_PROMPT_FILE);
        });

        // build the function
        var kernel = this.context.IsForInference
            ? await this.kernelFactory.GetOrCreateKernelForInferenceAsync(context.LLMEndpointIndex, cancellationToken)
            : await this.kernelFactory.GetOrCreateKernelForEvaluationAsync(context.LLMEndpointIndex, cancellationToken);
        var func = kernel.CreateFunctionFromPrompt(
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
        var startTime = DateTime.UtcNow;
        var response = await kernel.InvokeAsync(
            func,
            new KernelArguments
            {
                { "history", history },
                { "query", input.UserQuery },
            },
            cancellationToken);
        var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

        // extract metadata
        if (response.Metadata is not null && response.Metadata.TryGetValue("Usage", out var promptFilterResultsObj))
        {
            var promptFilterResults = promptFilterResultsObj as CompletionsUsage;
            if (promptFilterResults is not null)
            {
                this.Usage.PromptTokenCount = promptFilterResults.PromptTokens;
                DiagnosticService.RecordPromptTokenCount(this.Usage.PromptTokenCount, this.config.LLM_MODEL_NAME);
                this.Usage.CompletionTokenCount = promptFilterResults.CompletionTokens;
                DiagnosticService.RecordCompletionTokenCount(this.Usage.CompletionTokenCount, this.config.LLM_MODEL_NAME);
            }
        }

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