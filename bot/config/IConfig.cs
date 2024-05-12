public interface IConfig
{
    int PORT { get; }
    string OPEN_TELEMETRY_CONNECTION_STRING { get; }
    string MEMORY_URL { get; }
    string INFERENCE_URL { get; }
    int CHARACTERS_PER_UPDATE { get; }
    string FINAL_STATUS { get; }
    int MAX_RETRY_ATTEMPTS { get; }
    int SECONDS_BETWEEN_RETRIES { get; }

    void Validate();
}