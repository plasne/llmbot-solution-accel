public interface IConfig
{
    int PORT { get; }
    string OPEN_TELEMETRY_CONNECTION_STRING { get; }
    string LLM_URI { get; }
    int CHARACTERS_PER_UPDATE { get; }

    void Validate();
}