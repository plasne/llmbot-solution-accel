using System;
using Microsoft.SemanticKernel;
using SharpToken;

namespace Inference;

public class PromptTokenCountFilter(string modelName, Action<int> onRendered) : IPromptFilter
{
    private readonly string modelName = modelName;
    private readonly Action<int> onRendered = onRendered;

    public void OnPromptRendering(PromptRenderingContext context)
    {
        // nothing to do
    }

    public void OnPromptRendered(PromptRenderedContext context)
    {
        var encoding = GptEncoding.GetEncodingForModel(modelName);
        var prompt = context.RenderedPrompt;
        var count = encoding.CountTokens(prompt);
        this.onRendered(count);
    }
}

