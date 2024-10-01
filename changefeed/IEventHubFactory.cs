namespace ChangeFeed;

using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;

/// <summary>
/// A factory to create producer and consumer clients for Event Hub.
/// </summary>
public interface IEventHubFactory
{
    /// <summary>
    /// Creates a producer client.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A producer client.</returns>
    EventHubProducerClient CreateProducerClient(string connectionString);

    /// <summary>
    /// Creates a consumer client.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="consumerGroup">The consumer group.</param>
    /// <returns>A consumer client.</returns>
    EventHubConsumerClient CreateConsumerClient(string connectionString, string consumerGroup);
}