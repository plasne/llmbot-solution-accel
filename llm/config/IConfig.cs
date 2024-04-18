public interface IConfig
{
    string LLM_DEPLOYMENT_NAME { get; }
    string EMBEDDING_DEPLOYMENT_NAME { get; }
    string LLM_ENDPOINT_URI { get; }
    string LLM_API_KEY { get; }
    string SEARCH_INDEX { get; }
    string SEARCH_ENDPOINT_URI { get; }
    string SEARCH_API_KEY { get; }
    string SEARCH_SEMANTIC_CONFIG { get; }

    void Validate();
}