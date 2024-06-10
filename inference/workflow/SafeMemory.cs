using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inference;

public class SafeMemory : IMemory
{
    private readonly Dictionary<string, object> store = [];
    private readonly SemaphoreSlim semaphore = new(1);

    public async Task<T> GetOrSet<T>(string key, Func<T, Task>? onGet, Func<Task<T>> onSet)
    {
        await this.semaphore.WaitAsync();
        try
        {
            if (this.store.TryGetValue(key, out var obj))
            {
                var value = (T)obj;
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
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task Set<T>(string key, T value)
    {
        await this.semaphore.WaitAsync();
        try
        {
            this.store.Add(key, value!);
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public Task<bool> TryGet<T>(string key, out T value)
    {
        var success = this.store.TryGetValue(key, out var obj);
        value = success ? (T)obj! : default!;
        return Task.FromResult(success);
    }
}