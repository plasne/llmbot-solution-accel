using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedChat;

public interface IWorkflowContext
{
    event Func<string?, string?, Intent, List<Citation>?, int, int, Task> OnStream;

    public Task Stream(
        string? status = null,
        string? message = null,
        Intent intent = Intent.Unset,
        List<Citation>? citations = null,
        int promptTokens = 0,
        int completionTokens = 0);
}