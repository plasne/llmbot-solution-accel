using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SharpToken;

public class PromptTokenCountFilter(string modelName, string step, ILogger logger) : IPromptFilter
{
    private readonly string modelName = modelName;
    private readonly string step = step;
    private readonly ILogger logger = logger;

    public void OnPromptRendering(PromptRenderingContext context)
    {
        this.logger.LogInformation("prompt token count filter attached to step {step}.", this.step);
    }

    public void OnPromptRendered(PromptRenderedContext context)
    {
        var encoding = GptEncoding.GetEncodingForModel(modelName);
        var prompt = context.RenderedPrompt;

        DiagnosticService.RecordPromptTokenCount(encoding.CountTokens(prompt), modelName, this.step);
    }
}

