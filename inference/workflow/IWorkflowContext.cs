using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Models.Memory;

namespace Inference;

public interface IWorkflowContext
{
    event Func<string?, string?, Intents, List<Citation>?, int, int, Task> OnStream;

    public Task Stream(
        string? status = null,
        string? message = null,
        Intents intent = Intents.UNKNOWN,
        List<Citation>? citations = null,
        int promptTokens = 0,
        int completionTokens = 0);
}