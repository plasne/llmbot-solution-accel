using System;
using Microsoft.SemanticKernel;
using SharpToken;

public class PromptTokenCountFilter : IPromptFilter
{
    private readonly string _modelId;
    private readonly string _step;

    public PromptTokenCountFilter(string modelId, string step)
    {
        _modelId = modelId;
        _step = step;

    }
    public void OnPromptRendering(PromptRenderingContext context)
    {
        Console.WriteLine($"Rendering prompt for {this._step}");
    }

    public void OnPromptRendered(PromptRenderedContext context)
    {
        var encoding = GptEncoding.GetEncodingForModel(_modelId);
        var prompt = context.RenderedPrompt;

        DiagnosticService.RecordPromptTokenCount(encoding.CountTokens(prompt), this._step);
    }
}

