namespace ChangeFeed;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// This class provides extension methods.
/// </summary>
public static class Extensions
{
    private static readonly Random Rng = new();

    /// <summary>
    /// Shuffles the members of a list.
    /// </summary>
    /// <typeparam name="T">The type of members.</typeparam>
    /// <param name="list">The list.</param>
    /// <returns>The same list.</returns>
    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }

        return list;
    }

    /// <summary>
    /// This adds an Event Hub change feed and the associated configuration.
    /// </summary>
    /// <typeparam name="T">The type of the configuration object in the service collection.</typeparam>
    /// <param name="services">The service collection.</param>
    public static void AddEventHubChangeFeed<T>(this IServiceCollection services)
    {
        services.TryAddSingleton(provider => (IEventHubChangeFeedConfig)(provider.GetService<T>() ?? throw new Exception($"cannot resolve type {typeof(T).Name}")));
        services.TryAddSingleton<IEventHubFactory, EventHubFactory>();
        services.TryAddSingleton<IChangeFeed, EventHubChangeFeed>();
    }
}