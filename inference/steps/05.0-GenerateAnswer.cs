using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Shared;
using SharpToken;

namespace Inference;

public partial class GenerateAnswer(
    IConfig config,
    IWorkflowContext context,
    KernelFactory kernelFactory,
    IMemory memory,
    ILogger<GenerateAnswer> logger)
    : BaseStep<IntentAndData, Answer>(logger)
{
    private readonly IConfig config = config;
    private readonly IWorkflowContext context = context;
    private readonly KernelFactory kernelFactory = kernelFactory;
    private readonly IMemory memory = memory;

    public override string Name => "GenerateAnswer";

    public override async Task<Answer> ExecuteInternal(
        IntentAndData input,
        CancellationToken cancellationToken = default)
    {
        // validate input
        var query = input.Intent?.Query ?? input.Data?.UserQuery;
        if (string.IsNullOrEmpty(query))
        {
            throw new HttpException(400, "A query is required.");
        }

        // get or set the prompt template
        string promptFile = !string.IsNullOrEmpty(this.context.Parameters?.CHAT_PROMPT_FILE)
            ? this.context.Parameters.CHAT_PROMPT_FILE
            : this.config.CHAT_PROMPT_FILE;
        this.LogDebug($"using prompt file: {promptFile}...");
        string template = await this.memory.GetOrSet($"prompt:{promptFile}", null, () =>
        {
            return File.ReadAllTextAsync(promptFile);
        });

        // get or set the temperature
        double temperature = this.context.Parameters?.CHAT_TEMPERATURE is not null
            ? (double)this.context.Parameters.CHAT_TEMPERATURE
            : (double)this.config.CHAT_TEMPERATURE;
        this.LogDebug($"using temperature: {temperature:0.0}...");

        // build the function
        var kernel = this.context.IsForInference
            ? await this.kernelFactory.GetOrCreateKernelForInferenceAsync(context.LLMEndpointIndex, cancellationToken)
            : await this.kernelFactory.GetOrCreateKernelForEvaluationAsync(context.LLMEndpointIndex, cancellationToken);
        var func = kernel.CreateFunctionFromPrompt(
            new PromptTemplateConfig
            {
                Template = template,
                TemplateFormat = "handlebars",
            },
            new HandlebarsPromptTemplateFactory()
        );

        // build the history
        ChatHistory history = input.Data?.History?.ToChatHistory() ?? [];

        // get the responses
        var contextString = (input.Data?.Context is not null)
            ? string.Join("\n", input.Data.Context.Select(x => x.Text))
            : string.Empty;

        // execute
        var startTime = DateTime.UtcNow;
        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = temperature,
            Seed = this.config.CHAT_SEED,
        };
        var args = new KernelArguments(settings)
            {
                { "history", history },
                { "query", query },
                { "context", contextString }
            };
        var response = kernel.InvokeStreamingAsync(
            func,
            args,
            cancellationToken);

        // stream each fragment
        var buffer = new StringBuilder();
        await foreach (var fragment in response)
        {
            buffer.Append(fragment.ToString());
            await this.context.Stream("Generating answer...", fragment.ToString());
        }
        var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

        // record prompt token count using a prompt filter that adds an argument
        if (args.TryGetValue("internaluse:prompt-token-count", out var promptTokenCountObj) && promptTokenCountObj is int promptTokenCount)
        {
            this.Usage.PromptTokenCount = promptTokenCount;
            DiagnosticService.RecordPromptTokenCount(this.Usage.PromptTokenCount, this.config.LLM_MODEL_NAME);
        }

        // record completion token count using sharpToken
        var encoding = GptEncoding.GetEncoding(this.config.LLM_ENCODING_MODEL);
        this.Usage.CompletionTokenCount = encoding.CountTokens(buffer.ToString());
        DiagnosticService.RecordCompletionTokenCount(this.Usage.CompletionTokenCount, this.config.LLM_MODEL_NAME);

        // record tokens per second
        var tokensPerSecond = this.Usage.CompletionTokenCount / elapsedSeconds;
        DiagnosticService.RecordTokensPerSecond(tokensPerSecond, this.config.LLM_MODEL_NAME);

        // emit citations in order of relevance
        var citationIds = new HashSet<string>(MatchRef().Matches(buffer.ToString()).Select(m => m.Value));
        List<Context> citations = [];
        input.Data?.Context?.ForEach(x =>
        {
            if (citationIds.Contains($"[{x.Id}]")) citations.Add(x);
        });

        // send response
        await this.context.Stream("Generated.", citations: citations, promptTokens: this.Usage.PromptTokenCount, completionTokens: this.Usage.CompletionTokenCount);
        return new Answer { Text = buffer.ToString(), Context = citations };
    }

    [GeneratedRegex(@"\[ref\d+\]")]
    private static partial Regex MatchRef();
}