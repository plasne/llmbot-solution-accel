using System;
using System.Threading.Tasks;

public class Context : IContext
{
    public event Func<string?, string?, Task>? OnStream;

    public Task Stream(string? status, string? message)
    {
        return this.OnStream is not null
            ? this.OnStream(status, message)
            : Task.CompletedTask;
    }
}