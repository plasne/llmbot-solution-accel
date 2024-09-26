using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Models.Memory;

namespace Inference;

public class WorkflowContext(IConfig config, IServiceContext context) : IWorkflowContext
{
    public readonly KernelIndex kernelIndex = context.KernelIndex;
    private bool isForInference;
    private bool isForEvaluation;

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

    public KernelIndex KernelIndex
    {
        get => this.kernelIndex;
    }

    public IConfig Config { get; set; } = config;

    public WorkflowRequest? WorkflowRequest { get; set; }

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