using System;
using System.Threading.Tasks;

namespace Inference;

public interface IMemory
{
    public Task<T> GetOrSet<T>(string key, Func<T, Task>? onGet, Func<Task<T>> onSet);
    public Task Set<T>(string key, T value);
    public Task<bool> TryGet<T>(string key, out T value);
}