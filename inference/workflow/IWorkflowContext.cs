using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Shared.Models.Memory;

namespace Inference;

public interface IWorkflowContext
{
    public bool IsForInference { get; set; }
    public bool IsForEvaluation { get; set; }
    public Task<Kernel?> GetLlmKernelAsync(CancellationToken cancellationToken = default);
    public Task<Kernel?> GetEmbeddingKernelAsync(CancellationToken cancellationToken = default);
    public IConfig Config { get; set; }
    public WorkflowRequest? WorkflowRequest { get; set; }

    event Func<string?, string?, Intents, List<Context>?, int, int, int, Task> OnStream;

    public Task Stream(
        string? status = null,
        string? message = null,
        Intents intent = Intents.UNKNOWN,
        List<Context>? citations = null,
        int promptTokens = 0,
        int completionTokens = 0,
        int embeddingTokens = 0);
}