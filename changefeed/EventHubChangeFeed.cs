namespace ChangeFeed;

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;

/// <summary>
/// An implementation of IChangeFeed for Event Hub.
/// </summary>
public class EventHubChangeFeed : IChangeFeed
{
    private readonly IEventHubChangeFeedConfig config;
    private readonly IEventHubFactory factory;
    private readonly ILogger<EventHubChangeFeed> logger;
    private readonly SemaphoreSlim clientLock = new(1, 1);

    private EventHubProducerClient? producerClient;
    private EventHubConsumerClient? consumerClient;
    private string? consumerGroup;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubChangeFeed"/> class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="factory">A factory to create Event Hub clients.</param>
    /// <param name="logger">A logger.</param>
    public EventHubChangeFeed(
        IEventHubChangeFeedConfig config,
        IEventHubFactory factory,
        ILogger<EventHubChangeFeed> logger)
    {
        this.config = config;
        this.factory = factory;
        this.logger = logger;
    }

    /// <inheritdoc />
    public event IChangeFeed.OnNotifiedDelegateAsync? OnNotifiedAsync;

    /// <inheritdoc />
    public async Task NotifyAsync(string payload, CancellationToken cancellationToken = default)
    {
        // get a client
        var client = await this.GetOrCreateProducerClientAsync(cancellationToken);

        // send the message
        var bytes = Encoding.UTF8.GetBytes(payload);
        await client.SendAsync([new EventData(bytes)], cancellationToken);
    }

    /// <inheritdoc />
    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // get a client
            var client = await this.GetOrCreateConsumerClientAsync(reset: false, cancellationToken);

            // get a list of partitions
            this.logger.LogDebug("looking for partitions in the change feed...");
            var partitionIds = await client.GetPartitionIdsAsync(cancellationToken);
            this.logger.LogDebug("found {p} partitions in the change feed.", partitionIds.Length);

            // start listening to messages from all partitions
            foreach (var partitionId in partitionIds)
            {
                _ = this.GetEventsFromPartitionAsync(partitionId, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    /// <summary>
    /// This is called to dispose of the resources used by the component.
    /// </summary>
    /// <returns>A ValueTask for indicating completion.</returns>
    public async ValueTask DisposeAsync()
    {
        await this.DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The actual disposal of resources.
    /// </summary>
    /// <returns>A ValueTask for indicating completion.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (this.producerClient is not null)
        {
            await this.producerClient.DisposeAsync().ConfigureAwait(false);
        }

        if (this.consumerClient is not null)
        {
            await this.consumerClient.DisposeAsync().ConfigureAwait(false);
        }

        this.producerClient = null;
        this.consumerClient = null;
    }

    private async Task<EventHubProducerClient> GetOrCreateProducerClientAsync(CancellationToken cancellationToken)
    {
        // ensure this is single threaded
        await this.clientLock.WaitAsync(cancellationToken);
        try
        {
            // create the producer client if there isn't one
            this.producerClient ??= this.factory.CreateProducerClient(this.config.CHANGEFEED_CONNSTRING);
            return this.producerClient;
        }
        finally
        {
            this.clientLock.Release();
        }
    }

    private async Task<EventHubConsumerClient> GetOrCreateConsumerClientAsync(bool reset, CancellationToken cancellationToken)
    {
        // ensure this is single threaded
        await this.clientLock.WaitAsync(cancellationToken);
        try
        {
            // create the consumer if there isn't one; reset if required
            if (this.consumerClient is null || reset)
            {
                // shuffle the consumer groups; ignore the existing one
                var consumerGroups = this.config.CHANGEFEED_CONSUMER_GROUPS
                    .Where(x => x != this.consumerGroup)
                    .ToList()
                    .Shuffle();
                if (!consumerGroups.Any())
                {
                    this.logger.LogCritical("there were no valid consumer groups to use for the change feed.");
                    Environment.Exit(10000); // no valid consumer groups
                }

                // dispose of any existing client
                if (this.consumerClient is not null)
                {
                    await this.consumerClient.DisposeAsync();
                }

                // create the new client
                this.consumerGroup = consumerGroups.First();
                this.logger.LogInformation("the change feed will connect on consumer group {cg}.", this.consumerGroup);
                this.consumerClient = this.factory.CreateConsumerClient(this.config.CHANGEFEED_CONNSTRING, this.consumerGroup);
            }

            return this.consumerClient;
        }
        finally
        {
            this.clientLock.Release();
        }
    }

    private async Task GetEventsFromPartitionAsync(string partitionId, CancellationToken cancellationToken)
    {
        var reset = false;
        while (true)
        {
            try
            {
                // listen to events on a partition
                var position = EventPosition.Latest;
                var client = await this.GetOrCreateConsumerClientAsync(reset, cancellationToken);
                reset = false;
                this.logger.LogInformation("started listening to partition {p} for the change feed...", partitionId);
                await foreach (var evt in client.ReadEventsFromPartitionAsync(partitionId, position, cancellationToken))
                {
                    if (evt.Data is not null)
                    {
                        position = EventPosition.FromOffset(evt.Data.Offset, isInclusive: false);
                        if (this.OnNotifiedAsync is not null)
                        {
                            await this.OnNotifiedAsync(
                                this,
                                payload: evt.Data.EventBody.ToString(),
                                cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (EventHubsException ex) when (ex.Reason == EventHubsException.FailureReason.QuotaExceeded)
            {
                this.logger.LogWarning("the change feed has too many consumers, another will be chosen if possible...");
                reset = true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "an exception was thrown in EventHubChangeFeed.GetEventsFromPartition()...");
            }

            // delay before retry
            try
            {
                await Task.Delay(1000, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
