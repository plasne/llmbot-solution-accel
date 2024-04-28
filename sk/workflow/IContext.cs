using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedChat;

public interface IContext
{
    event Func<string?, string?, List<Citation>?, int, int, Task> OnStream;

    event Func<Intent, string?, Task> OnTerminate;

    public Task Stream(
        string? status = null,
        string? message = null,
        List<Citation>? citations = null,
        int promptTokens = 0,
        int completionTokens = 0);

    public Task Terminate(Intent intent, string? message = null);
}