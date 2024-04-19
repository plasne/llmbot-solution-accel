using System;
using System.Threading.Tasks;

public interface IContext
{
    event Func<string, Task> OnStream;

    event Func<string, Task> OnStatus;

    public Task Stream(string message);

    public Task SetStatus(string status);
}