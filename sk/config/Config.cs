using NetBricks;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.GRPC_PORT = config.Get<string>("GRPC_PORT").AsInt(() => 5210);
        this.WEB_PORT = config.Get<string>("WEB_PORT").AsInt(() => 5211);
        this.OPEN_TELEMETRY_CONNECTION_STRING = config.Get<string>("OPEN_TELEMETRY_CONNECTION_STRING");
        this.MEMORY_TERM = config.Get<string>("MEMORY_TERM").AsEnum<MemoryTerm>(() => MemoryTerm.Long);
        this.LLM_DEPLOYMENT_NAME = config.Get<string>("LLM_DEPLOYMENT_NAME");
        this.EMBEDDING_DEPLOYMENT_NAME = config.Get<string>("EMBEDDING_DEPLOYMENT_NAME");
        this.LLM_ENDPOINT_URI = config.Get<string>("LLM_ENDPOINT_URI");
        this.LLM_API_KEY = config.Get<string>("LLM_API_KEY");
        this.SEARCH_INDEX = config.Get<string>("SEARCH_INDEX");
        this.SEARCH_ENDPOINT_URI = config.Get<string>("SEARCH_ENDPOINT_URI");
        this.SEARCH_API_KEY = config.Get<string>("SEARCH_API_KEY");
        this.SEARCH_SEMANTIC_CONFIG = config.Get<string>("SEARCH_SEMANTIC_CONFIG").AsString(() => "default");
        this.AZURE_STORAGE_ACCOUNT_NAME = config.Get<string>("AZURE_STORAGE_ACCOUNT_NAME");
        this.AZURE_STORAGE_INFERENCE_QUEUE = config.Get<string>("AZURE_STORAGE_INFERENCE_QUEUE");
        this.AZURE_STORAGE_EVALUATION_QUEUE = config.Get<string>("AZURE_STORAGE_EVALUATION_QUEUE");
    }

    public int GRPC_PORT { get; }

    public int WEB_PORT { get; }

    public string OPEN_TELEMETRY_CONNECTION_STRING { get; }

    public MemoryTerm MEMORY_TERM { get; }

    public string LLM_DEPLOYMENT_NAME { get; }

    public string EMBEDDING_DEPLOYMENT_NAME { get; }

    public string LLM_ENDPOINT_URI { get; }

    public string LLM_API_KEY { get; }

    public string SEARCH_INDEX { get; }

    public string SEARCH_ENDPOINT_URI { get; }

    public string SEARCH_API_KEY { get; }

    public string SEARCH_SEMANTIC_CONFIG { get; }

    public string AZURE_STORAGE_ACCOUNT_NAME { get; }

    public string AZURE_STORAGE_INFERENCE_QUEUE { get; }

    public string AZURE_STORAGE_EVALUATION_QUEUE { get; }

    public void Validate()
    {
        this.config.Require("GRPC_PORT", this.GRPC_PORT);
        this.config.Require("WEB_PORT", this.WEB_PORT);
        this.config.Require("OPEN_TELEMETRY_CONNECTION_STRING", this.OPEN_TELEMETRY_CONNECTION_STRING, hideValue: true);
        this.config.Require("MEMORY_TERM", this.MEMORY_TERM.ToString());
        this.config.Require("LLM_DEPLOYMENT_NAME", this.LLM_DEPLOYMENT_NAME);
        this.config.Require("EMBEDDING_DEPLOYMENT_NAME", this.EMBEDDING_DEPLOYMENT_NAME);
        this.config.Require("LLM_ENDPOINT_URI", this.LLM_ENDPOINT_URI);
        this.config.Require("LLM_API_KEY", this.LLM_API_KEY, hideValue: true);
        this.config.Require("SEARCH_INDEX", this.SEARCH_INDEX);
        this.config.Require("SEARCH_ENDPOINT_URI", this.SEARCH_ENDPOINT_URI);
        this.config.Require("SEARCH_API_KEY", this.SEARCH_API_KEY, hideValue: true);
        this.config.Require("SEARCH_SEMANTIC_CONFIG", this.SEARCH_SEMANTIC_CONFIG);
        this.config.Optional("AZURE_STORAGE_ACCOUNT_NAME", this.AZURE_STORAGE_ACCOUNT_NAME);
        this.config.Optional("AZURE_STORAGE_INFERENCE_QUEUE", this.AZURE_STORAGE_INFERENCE_QUEUE);
        this.config.Optional("AZURE_STORAGE_EVALUATION_QUEUE", this.AZURE_STORAGE_EVALUATION_QUEUE);
    }
}