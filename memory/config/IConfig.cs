using Iso8601DurationHelper;

namespace Memory;

public interface IConfig
{
    int PORT { get; }
    string OPEN_TELEMETRY_CONNECTION_STRING { get; }
    int SQL_SERVER_MAX_RETRY_ATTEMPTS { get; }
    int SQL_SERVER_SECONDS_BETWEEN_RETRIES { get; }
    string SQL_SERVER_HISTORY_SERVICE_CONNSTRING { get; }
    Duration DEFAULT_RETENTION { get; }

    void Validate();
}