using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedChat;

public class Context : IContext
{
    public event Func<string?, string?, List<Citation>?, Task>? OnStream;

    public Task Stream(string? status, string? message, List<Citation>? citations)
    {
        return this.OnStream is not null
            ? this.OnStream(status, message, citations)
            : Task.CompletedTask;
    }
}