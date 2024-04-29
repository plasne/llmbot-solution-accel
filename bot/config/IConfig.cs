using Iso8601DurationHelper;

public interface IConfig
{
    int PORT { get; }
    string OPEN_TELEMETRY_CONNECTION_STRING { get; }
    string LLM_URI { get; }
    int CHARACTERS_PER_UPDATE { get; }
    string FINAL_STATUS { get; }
    string SQL_SERVER_HISTORY_SERVICE_CONNSTRING { get; }
    int SQL_SERVER_MAX_RETRY_ATTEMPTS { get; }
    int SQL_SERVER_SECONDS_BETWEEN_RETRIES { get; }
    Duration DEFAULT_RETENTION { get; }

    void Validate();
}