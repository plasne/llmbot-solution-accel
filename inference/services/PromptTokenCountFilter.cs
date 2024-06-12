using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using SharpToken;

namespace Inference;

public class PromptTokenCountFilter(IConfig config) : IPromptRenderFilter
{
    private readonly IConfig config = config;

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        await next(context);
        var encoding = GptEncoding.GetEncoding(this.config.LLM_ENCODING_MODEL);
        var count = encoding.CountTokens(context.RenderedPrompt);
        context.Arguments.Add("internaluse:prompt-token-count", count);
    }
}