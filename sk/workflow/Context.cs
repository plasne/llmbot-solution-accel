using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedChat;

public class Context : IContext
{
    public event Func<string?, string?, Intent, List<Citation>?, int, int, Task>? OnStream;

    public Task Stream(
        string? status = null,
        string? message = null,
        Intent intent = Intent.Unset,
        List<Citation>? citations = null,
        int promptTokens = 0,
        int completionTokens = 0)
    {
        return this.OnStream is not null
            ? this.OnStream(status, message, intent, citations, promptTokens, completionTokens)
            : Task.CompletedTask;
    }
}