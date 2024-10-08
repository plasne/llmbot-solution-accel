using System.Collections.Generic;
using SharpToken;

namespace Inference;

public interface IConfig
{
    int GRPC_PORT { get; }
    int WEB_PORT { get; }
    MemoryTerm MEMORY_TERM { get; }
    string OPEN_TELEMETRY_CONNECTION_STRING { get; }
    List<ModelConnectionDetails> LLM_CONNECTION_STRINGS { get; }
    string LLM_MODEL_NAME { get; }
    GptEncoding? LLM_ENCODING { get; set; }
    List<ModelConnectionDetails> EMBEDDING_CONNECTION_STRINGS { get; }
    string EMBEDDING_MODEL_NAME { get; }
    GptEncoding? EMBEDDING_ENCODING { get; set; }
    string SEARCH_INDEX { get; }
    string SEARCH_ENDPOINT_URI { get; }
    string SEARCH_API_KEY { get; }
    string SEARCH_SEMANTIC_RERANK_CONFIG { get; }
    SearchMode SEARCH_MODE { get; }
    string[] SEARCH_VECTOR_FIELDS { get; }
    int SEARCH_KNN { get; }
    bool SEARCH_VECTOR_EXHAUST_KNN { get; }
    string[] SEARCH_SELECT_FIELDS { get; }
    string SEARCH_TRANSFORM_FILE { get; }
    string PICK_DOCS_URL_FIELD { get; }
    int MAX_CONCURRENT_SEARCHES { get; }
    int MAX_SEARCH_QUERIES_PER_INTENT { get; }
    decimal MIN_RELEVANCE_SEARCH_SCORE { get; }
    decimal MIN_RELEVANCE_RERANK_SCORE { get; }
    int SEARCH_TOP { get; }
    string INTENT_PROMPT_FILE { get; }
    string CHAT_PROMPT_FILE { get; }
    decimal INTENT_TEMPERATURE { get; }
    decimal CHAT_TEMPERATURE { get; }
    long? INTENT_SEED { get; }
    long? CHAT_SEED { get; }
    string MEMORY_URL { get; }
    int MAX_RETRY_ATTEMPTS { get; }
    int SECONDS_BETWEEN_RETRIES { get; }
    int MAX_TIMEOUT_IN_SECONDS { get; }
    int SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT { get; }
    bool EXIT_WHEN_OUT_OF_DOMAIN { get; }
    bool EXIT_WHEN_NO_DOCUMENTS { get; }
    bool EXIT_WHEN_NO_CITATIONS { get; }

    void Validate();
}