using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedChat;

public class Context : IContext
{
    public event Func<string?, string?, List<Citation>?, int, int, Task>? OnStream;
    public event Func<Intent, string?, Task>? OnTerminate;

    public Task Stream(
        string? status = null,
        string? message = null,
        List<Citation>? citations = null,
        int promptTokens = 0,
        int completionTokens = 0)
    {
        return this.OnStream is not null
            ? this.OnStream(status, message, citations, promptTokens, completionTokens)
            : Task.CompletedTask;
    }

    public Task Terminate(Intent intent, string? message = null)
    {
        return this.OnTerminate is not null
            ? this.OnTerminate(intent, message)
            : Task.CompletedTask;
    }
}