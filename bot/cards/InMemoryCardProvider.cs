using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.Templating;

namespace Bot;

public class InMemoryCardProvider : ICardProvider
{
    private readonly ConcurrentDictionary<string, Lazy<Task<AdaptiveCardTemplate>>> cards = new();
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<AdaptiveCardTemplate> GetTemplate(string name)
    {
        var lazyTask = this.cards.GetOrAdd(name, n => new Lazy<Task<AdaptiveCardTemplate>>(() => LoadCardAsync(n)));

        if (!lazyTask.IsValueCreated)
        {
            await semaphore.WaitAsync();
            try
            {
                // double-check that it hasn't been created yet
                if (!lazyTask.IsValueCreated)
                {
                    await lazyTask.Value;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        return await lazyTask.Value;
    }

    private static async Task<AdaptiveCardTemplate> LoadCardAsync(string name)
    {
        var json = await File.ReadAllTextAsync($"./cards/{name}.json");
        return new AdaptiveCardTemplate(json);
    }
}