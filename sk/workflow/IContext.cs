using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedChat;

public interface IContext
{
    event Func<string?, string?, List<Citation>?, Task> OnStream;

    public Task Stream(string? status, string? message = null, List<Citation>? citations = null);
}