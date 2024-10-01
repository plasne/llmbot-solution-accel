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

public partial class GenerateAnswerWithLlm(
    IWorkflowContext context,
    KernelFactory kernelFactory,
    IMemory memory,
    ILogger<GenerateAnswerWithLlm> logger)
    : BaseStep<IntentAndData, Answer>(logger), IGenerateAnswer
{
    private readonly IWorkflowContext context = context;
    private readonly KernelFactory kernelFactory = kernelFactory;
    private readonly IMemory memory = memory;

    public override string Name => "GenerateAnswerWithLlm";

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
        string promptFile = this.context.Config.CHAT_PROMPT_FILE;
        this.LogDebug($"using prompt file: {promptFile}...");
        string template = await this.memory.GetOrSet($"prompt:{promptFile}", null, () =>
        {
            return File.ReadAllTextAsync(promptFile);
        });

        // get or set the temperature
        double temperature = (double)this.context.Config.CHAT_TEMPERATURE;
        this.LogDebug($"using temperature: {temperature:0.0}...");

        // build the function
        var kernel = await this.context.GetLlmKernelAsync();
        if (kernel is null) throw new HttpException(500, "no LLM kernel is available.");
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
            Seed = this.context.Config.CHAT_SEED,
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
        var citations = new Dictionary<string, Context>();
        await foreach (var fragment in response)
        {
            if (buffer.Length == 0 && fragment.ToString() == "NULL")
                break;
            buffer.Append(fragment.ToString());
            var citationIds = new HashSet<string>(MatchRef().Matches(buffer.ToString()).Select(m => m.Value));
            input.Data?.Context?.ForEach(x =>
            {
                if (citationIds.Contains($"[{x.Id}]")) citations.TryAdd(x.Id, x);
            });
            await this.context.Stream("Generating answer...", fragment.ToString(), citations: citations.Values.ToList());
        }
        var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

        // record prompt token count using a prompt filter that adds an argument
        if (args.TryGetValue("internaluse:prompt-token-count", out var promptTokenCountObj) && promptTokenCountObj is int promptTokenCount)
        {
            this.Usage.PromptTokenCount = promptTokenCount;
            DiagnosticService.RecordPromptTokenCount(this.Usage.PromptTokenCount, this.context.Config.LLM_MODEL_NAME);
        }

        // record completion token count using sharpToken
        if (this.context.Config.LLM_ENCODING is not null)
        {
            this.Usage.CompletionTokenCount = this.context.Config.LLM_ENCODING.CountTokens(buffer.ToString());
            DiagnosticService.RecordCompletionTokenCount(this.Usage.CompletionTokenCount, this.context.Config.LLM_MODEL_NAME);
        }

        // record tokens per second
        var tokensPerSecond = this.Usage.CompletionTokenCount / elapsedSeconds;
        DiagnosticService.RecordTokensPerSecond(tokensPerSecond, this.context.Config.LLM_MODEL_NAME);

        // send response
        await this.context.Stream("Generated.", promptTokens: this.Usage.PromptTokenCount, completionTokens: this.Usage.CompletionTokenCount);
        this.Continue = buffer.Length > 0 && (!this.context.Config.EXIT_WHEN_NO_CITATIONS || citations.Any());
        return new Answer { Text = buffer.ToString(), Context = citations.Values.ToList() };
    }

    [GeneratedRegex(@"\[ref\d+\]")]
    private static partial Regex MatchRef();
}