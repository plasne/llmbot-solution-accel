public interface IConfig
{
    public string LLM_DEPLOYMENT_NAME { get; }
    public string LLM_ENDPOINT_URI { get; }
    public string LLM_API_KEY { get; }

    public void Validate();
}