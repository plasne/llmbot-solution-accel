namespace ChangeFeed;

/// <summary>
/// In interface that can be included in configurations to add what is needed for an Event Hub Change Feed.
/// </summary>
public interface IEventHubChangeFeedConfig
{
    /// <summary>
    /// Gets the connection string for the change feed.
    /// </summary>
    public string CHANGEFEED_CONNSTRING { get; }

    /// <summary>
    /// Gets the name of the consumer groups. If any are full (5 non-epoch consumers are allowed), another will be tried.
    /// </summary>
    public string[] CHANGEFEED_CONSUMER_GROUPS { get; }
}