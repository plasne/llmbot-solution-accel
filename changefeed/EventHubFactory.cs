namespace ChangeFeed;

using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;

/// <summary>
/// An implementation of IEventHubFactory.
/// </summary>
[ExcludeFromCodeCoverage]
public class EventHubFactory : IEventHubFactory
{
    /// <inheritdoc />
    public EventHubConsumerClient CreateConsumerClient(string connectionString, string consumerGroup)
    {
        return new EventHubConsumerClient(consumerGroup, connectionString);
    }

    /// <inheritdoc />
    public EventHubProducerClient CreateProducerClient(string connectionString)
    {
        return new EventHubProducerClient(connectionString);
    }
}