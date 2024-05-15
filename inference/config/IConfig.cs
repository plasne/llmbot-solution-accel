namespace Inference;

public interface IConfig
{
    int GRPC_PORT { get; }
    int WEB_PORT { get; }
    MemoryTerm MEMORY_TERM { get; }
    string OPEN_TELEMETRY_CONNECTION_STRING { get; }
    string LLM_DEPLOYMENT_NAME { get; }
    string EMBEDDING_DEPLOYMENT_NAME { get; }
    string LLM_ENDPOINT_URI { get; }
    string LLM_API_KEY { get; }
    string LLM_MODEL_NAME { get; }
    string LLM_ENCODING_MODEL { get; set; }
    string SEARCH_INDEX { get; }
    string SEARCH_ENDPOINT_URI { get; }
    string SEARCH_API_KEY { get; }
    string SEARCH_SEMANTIC_CONFIG { get; }
    string[] SEARCH_VECTOR_FIELDS { get; }
    string MEMORY_URL { get; }
    int MAX_RETRY_ATTEMPTS { get; }
    int SECONDS_BETWEEN_RETRIES { get; }
    int MAX_TIMEOUT_IN_SECONDS { get; }

    void Validate();
}