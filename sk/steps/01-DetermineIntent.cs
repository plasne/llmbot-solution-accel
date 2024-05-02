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
using DistributedChat;
using Azure;
using Microsoft.Extensions.Options;
using Azure.Search.Documents.Models;
using System.Text.Json;
using HandlebarsDotNet;
using Xunit.Sdk;

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

        // build the history
        ChatHistory history = input.History?.ToChatHistory() ?? [];

        // render the prompt
        var f = new HandlebarsPromptTemplateFactory();
        var q = f.Create(new PromptTemplateConfig
        {
            Template = template,
            TemplateFormat = "handlebars"
        });
        var prompt = await q.RenderAsync(this.kernel, new()
            {
                { "history", history },
                { "query", input.UserQuery },
            }, cancellationToken: cancellationToken);

        var determineIntentDefinition = new ChatCompletionsFunctionToolDefinition()
        {
            Name = "determine_intent",
            Description = "Determines the intent of the user.",
            Parameters = BinaryData.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    Intent = new
                    {
                        Type = "string",
                        Enum = new[] { "UNKNOWN", "GREETING", "GOODBYE", "IN_DOMAIN", "OUT_OF_DOMAIN", "TOPIC_CHANGE" },
                    },
                    Query = new
                    {
                        Type = "string"
                    },
                    SearchQueries = new
                    {
                        Type = "array",
                        Items = new
                        {
                            Type = "string"
                        }
                    },
                    GameName = new
                    {
                        Type = "string"
                    },
                    Edition = new
                    {
                        Type = "string"
                    }
                },
                Required = new[] { "intent", "query", "search_queries" },
            },
            new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }),
        };

        var client = new OpenAIClient(new Uri(this.config.LLM_ENDPOINT_URI), new AzureKeyCredential(this.config.LLM_API_KEY));
        var options = new ChatCompletionsOptions
        {
            DeploymentName = this.config.LLM_DEPLOYMENT_NAME,
            ToolChoice = new ChatCompletionsToolChoice(determineIntentDefinition),
        };
        foreach (var message in prompt.ToChatRequestMessages())
        {
            options.Messages.Add(message);
        }
        options.Tools.Add(determineIntentDefinition);
        var response = await client.GetChatCompletionsAsync(options, cancellationToken);

        var choice = response.Value.Choices[0];
        if (choice.Message.ToolCalls is null || choice.Message.ToolCalls.Count != 1)
        {
            throw new Exception("Unexpected tool calls.");
        }

        var toolCall = choice.Message.ToolCalls[0] as ChatCompletionsFunctionToolCall;
        if (toolCall is null)
        {
            throw new Exception("tool call is null.");
        }
        var intent = JsonConvert.DeserializeObject<DeterminedIntent>(toolCall.Arguments);

        this.logger.LogWarning(JsonConvert.SerializeObject(choice));


        // execute
        var startTime = DateTime.UtcNow;
        var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

        // record completion token count using sharpToken
        /*
        var completionTokenCount = 0;
        if (response.Metadata is not null && response.Metadata.TryGetValue("Usage", out var usageOut) && usageOut is CompletionsUsage usage)
        {
            var encoding = GptEncoding.GetEncoding(this.config.LLM_ENCODING_MODEL);
            completionTokenCount = encoding.CountTokens(response.ToString());
            if (completionTokenCount != usage.CompletionTokens)
            {
                this.LogWarning("completion token count mismatch: {completionTokenCount} != {usage.CompletionTokens}");
            }
            DiagnosticService.RecordCompletionTokenCount(completionTokenCount, this.config.LLM_MODEL_NAME, this.GetType().Name);
        }

        // record tokens per second
        if (completionTokenCount > 0)
        {
            var tokensPerSecond = completionTokenCount / elapsedSeconds;
            DiagnosticService.RecordTokensPerSecond(tokensPerSecond, this.config.LLM_MODEL_NAME, this.GetType().Name);
        }
        */

        // deserialize the response
        // NOTE: this could maybe be a retry (transient fault)
        /*
        var intent = JsonConvert.DeserializeObject<DeterminedIntent>(response.ToString())
            ?? throw new HttpException(500, "Intent could not be deserialized.");
        */

        // if in debug mode, log the intent
        // this.logger.LogDebug(response.ToString());

        // send token counts
        // await this.context.Stream(promptTokens: promptTokenCount, completionTokens: completionTokenCount);

        // record to context
        return intent;
    }
}