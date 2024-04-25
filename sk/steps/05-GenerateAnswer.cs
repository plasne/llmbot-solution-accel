using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DistributedChat;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SharpToken;

public partial class GenerateAnswer(
    IConfig config,
    IContext context,
    Kernel kernel,
    IMemory memory,
    ILogger<GenerateAnswer> logger)
    : BaseStep<IntentAndData, Answer>(logger)
{
    private readonly IConfig config = config;
    private readonly IContext context = context;
    private readonly Kernel kernel = kernel;
    private readonly IMemory memory = memory;
    private readonly ILogger<GenerateAnswer> logger = logger;

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
        var func = this.kernel.CreateFunctionFromPrompt(
            new()
            {
                Template = template,
                TemplateFormat = "handlebars"
            },
            new HandlebarsPromptTemplateFactory()
        );

        // add prompt token count reporting
        kernel.PromptFilters.Add(new PromptTokenCountFilter(this.config.LLM_MODEL_NAME, this.GetType().Name, this.logger));

        // build the history
        ChatHistory history = input.Data?.History?.ToChatHistory() ?? [];

        // get the responses
        var contextString = (input.Data?.Content is not null)
            ? string.Join("\n", input.Data.Content.Select(x => x.Text))
            : string.Empty;

        // execute
        var startTime = DateTime.UtcNow;
        var response = this.kernel.InvokeStreamingAsync(
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
        var elapsedSeconds = (startTime - DateTime.UtcNow).TotalSeconds;

        // record completion token count using sharpToken
        var completionTokenCount = 0;
        var encoding = GptEncoding.GetEncoding(this.config.LLM_MODEL_ID);
        completionTokenCount = encoding.CountTokens(buffer.ToString());
        DiagnosticService.RecordCompletionTokenCount(completionTokenCount, this.config.LLM_MODEL_NAME, this.GetType().Name);

        // record tokens per second
        if (completionTokenCount > 0)
        {
            var tokensPerSecond = completionTokenCount / elapsedSeconds;
            DiagnosticService.RecordTokensPerSecond(tokensPerSecond, this.config.LLM_MODEL_NAME, this.GetType().Name);
        }

        // find citations
        Dictionary<string, Citation> citations = [];
        MatchCollection matches = MatchRef().Matches(buffer.ToString());
        foreach (Match match in matches)
        {
            if (!citations.ContainsKey(match.Value))
            {
                var content = input.Data?.Content?.Find(x => $"[{x.Citation?.Ref}]" == match.Value);
                if (content is not null && content.Citation is not null)
                {
                    citations.Add(match.Value, content.Citation);
                }
            }
        }

        // send response
        List<Citation> citationList = [.. citations.Values];
        await this.context.Stream("Generated.", citations: citationList);
        return new Answer { Text = buffer.ToString(), Citations = citationList };
    }

    [GeneratedRegex(@"\[ref\d+\]")]
    private static partial Regex MatchRef();
}