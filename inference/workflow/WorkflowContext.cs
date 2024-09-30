using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;
using Microsoft.SemanticKernel;
using Shared.Models.Memory;

namespace Inference;

public class WorkflowContext(IConfig config, KernelFactory kernelFactory) : IWorkflowContext
{
    private readonly KernelFactory kernelFactory = kernelFactory;
    private bool isForInference;
    private bool isForEvaluation;
    private Kernel? llmKernel;
    private Kernel? embeddingKernel;

    public bool IsForInference
    {
        get => this.isForInference;
        set
        {
            this.isForInference = value;
            this.isForEvaluation = !value;
        }
    }

    public bool IsForEvaluation
    {
        get => this.isForEvaluation;
        set
        {
            this.isForEvaluation = value;
            this.isForInference = !value;
        }
    }

    public IConfig Config { get; set; } = config;

    public WorkflowRequest? WorkflowRequest { get; set; }

    public async Task<Kernel?> GetLlmKernelAsync(CancellationToken cancellationToken = default)
    {
        return this.llmKernel ??= await this.kernelFactory.GetOrCreateLlmKernelAsync(IsForInference, cancellationToken);
    }

    public async Task<Kernel?> GetEmbeddingKernelAsync(CancellationToken cancellationToken = default)
    {
        return this.embeddingKernel ??= await this.kernelFactory.GetOrCreateEmbeddingKernelAsync(IsForInference, cancellationToken);
    }

    public event Func<string?, string?, Intents, List<Context>?, int, int, int, Task>? OnStream;

    public Task Stream(
        string? status = null,
        string? message = null,
        Intents intent = Intents.UNKNOWN,
        List<Context>? citations = null,
        int promptTokens = 0,
        int completionTokens = 0,
        int embeddingTokens = 0)
    {
        return this.OnStream is not null
            ? this.OnStream(status, message, intent, citations, promptTokens, completionTokens, embeddingTokens)
            : Task.CompletedTask;
    }
}