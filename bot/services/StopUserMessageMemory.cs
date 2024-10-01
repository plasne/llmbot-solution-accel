using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Bot;

public class StopUserMessageMemory(ILogger<StopUserMessageMemory> logger)
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> stopUserMessages = new();
    private readonly ILogger<StopUserMessageMemory> logger = logger;

    public void TryAdd(string activityId, CancellationTokenSource cancellationTokenSource)
    {
        stopUserMessages.TryAdd(activityId, cancellationTokenSource);
    }

    public bool TryRemove(string activityId, bool shouldCancel = true)
    {
        if (stopUserMessages.TryRemove(activityId, out CancellationTokenSource? cancellation))
        {
            if (shouldCancel)
            {
                logger.LogInformation("Cancelling the token for activityId {activityId}.", activityId);
                cancellation?.Cancel();
            }
            
            return true;
        }

        return false;
    }
}
