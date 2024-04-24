using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedChat;

public class Context : IContext
{
    public event Func<string?, string?, List<Citation>?, Task>? OnStream;
    public event Func<Intent, string?, Task>? OnTerminate;

    public Task Stream(string? status, string? message, List<Citation>? citations)
    {
        return this.OnStream is not null
            ? this.OnStream(status, message, citations)
            : Task.CompletedTask;
    }

    public Task Terminate(Intent intent, string? message = null)
    {
        return this.OnTerminate is not null
            ? this.OnTerminate(intent, message)
            : Task.CompletedTask;
    }
}