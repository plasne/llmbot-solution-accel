public interface IConfig
{
    string LLM_DEPLOYMENT_NAME { get; }
    string LLM_ENDPOINT_URI { get; }
    string LLM_API_KEY { get; }

    void Validate();
}