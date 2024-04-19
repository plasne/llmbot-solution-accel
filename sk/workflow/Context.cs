using System;
using System.Threading.Tasks;

public class Context : IContext
{
    public event Func<string, Task>? OnStatus;

    public event Func<string, Task>? OnStream;

    public Task SetStatus(string status)
    {
        return this.OnStatus is not null
            ? this.OnStatus(status)
            : Task.CompletedTask;
    }

    public Task Stream(string message)
    {
        return this.OnStream is not null
            ? this.OnStream(message)
            : Task.CompletedTask;
    }
}