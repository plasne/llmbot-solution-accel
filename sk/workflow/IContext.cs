using System;
using System.Threading.Tasks;

public interface IContext
{
    event Func<string?, string?, Task> OnStream;

    public Task Stream(string? status, string? message = null);
}