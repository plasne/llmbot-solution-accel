using System.Threading;
using System.Threading.Tasks;
using ChangeFeed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot;

/// <summary>
/// This hosted service allows for startup and shutdown activities related to the application itself.
/// </summary>
public class ChangeFeedLifecycle : IHostedService
{
    private readonly IEventHubChangeFeedConfig config;
    private readonly IChangeFeed changeFeed;
    private readonly CancellationTokenSource cts = new();
    private readonly ILogger<ChangeFeedLifecycle> logger;
    private readonly StopUserMessageMemory stopUserMessageMemory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeFeedLifecycle"/> class.
    /// </summary>
    /// <param name="config">The configuration for this application.</param>
    /// <param name="changeFeed">The change feed.</param>
    /// <param name="stopUserMessageMemory">Stores stop message in memory</param>
    /// <param name="logger">The logger.</param>
    public ChangeFeedLifecycle(IEventHubChangeFeedConfig config, IChangeFeed changeFeed, StopUserMessageMemory stopUserMessageMemory, ILogger<ChangeFeedLifecycle> logger)
    {
        this.config = config;
        this.changeFeed = changeFeed;
        this.stopUserMessageMemory = stopUserMessageMemory;
        this.logger = logger;
    }

    /// <summary>
    /// This method should contain all startup activities for the application.
    /// </summary>
    /// <param name="cancellationToken">A token that can be cancelled to abort startup.</param>
    /// <returns>A Task that is complete when the method is done.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // validate the configuration
        if (string.IsNullOrEmpty(config.CHANGEFEED_CONNSTRING))
        {
            return;
        }

        // add an event handler
        changeFeed.OnNotifiedAsync += (object sender, string payload, CancellationToken cancellationToken) =>
        {
            logger.LogDebug("received notification: '{payload}'.", payload);

            if (payload is not null && payload.StartsWith("STOP."))
            {
                stopUserMessageMemory.TryRemove(payload.Split(".")[1]);
            }

            return Task.CompletedTask;
        };

        // listen for changes
        await changeFeed.ListenAsync(cts.Token);
    }

    /// <summary>
    /// This method should contain all shutdown activities for the application.
    /// </summary>
    /// <param name="cancellationToken">A token that can be cancelled to abort startup.</param>
    /// <returns>A Task that is complete when the method is done.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        cts.Cancel();
        cts.Dispose();
        return Task.CompletedTask;
    }
}
