using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class VolatileMemory : IMemory
{
    private readonly Dictionary<string, object> store = new();

    public async Task<T> GetOrSet<T>(string key, Func<T, Task>? onGet, Func<Task<T>> onSet)
    {
        if (this.store.TryGetValue(key, out var obj))
        {
            var value = (T)obj!;
            if (onGet is not null)
            {
                await onGet(value);
            }
            return value;
        }
        else
        {
            var value = await onSet();
            this.store.Add(key, value!);
            return value;
        }
    }

    public Task Set<T>(string key, T value)
    {
        this.store.Add(key, value!);
        return Task.CompletedTask;
    }

    public Task<bool> TryGet<T>(string key, out T value)
    {
        var success = this.store.TryGetValue(key, out var obj);
        value = (T)obj!;
        return Task.FromResult(success);
    }
}