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
        string template = await this.memory.GetOrSet("prompt:chat", null, () =>
        {
            return File.ReadAllTextAsync("prompts/chat.txt");
        });

        // build the function
        var kernel = this.context.IsForInference
            ? await this.kernelFactory.GetOrCreateKernelForInferenceAsync(cancellationToken)
            : await this.kernelFactory.GetOrCreateKernelForEvaluationAsync(cancellationToken);
        var func = kernel.CreateFunctionFromPrompt(
            new()
            {
                Template = template,
                TemplateFormat = "handlebars"
            },
            new HandlebarsPromptTemplateFactory()
        );

        // add prompt token count reporting
        kernel.PromptFilters.Add(new PromptTokenCountFilter(this.config.LLM_MODEL_NAME, count =>
        {
            this.Usage.PromptTokenCount = count;
            DiagnosticService.RecordPromptTokenCount(count, this.config.LLM_MODEL_NAME);
        }));

        // build the history
        ChatHistory history = input.Data?.History?.ToChatHistory() ?? [];

        // get the responses
        var contextString = (input.Data?.Context is not null)
            ? string.Join("\n", input.Data.Context.Select(x => x.Text))
            : string.Empty;

        // execute
        var startTime = DateTime.UtcNow;
        var response = kernel.InvokeStreamingAsync(
            func,
            new()
            {
                { "history", history },
                { "query", query },
                { "context", contextString }
            },
            cancellationToken);

        // stream each fragment
        var buffer = new StringBuilder();
        await foreach (var fragment in response)
        {
            buffer.Append(fragment.ToString());
            await this.context.Stream("Generating answer...", fragment.ToString());
        }
        var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

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