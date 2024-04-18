using NetBricks;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.LLM_DEPLOYMENT_NAME = config.Get<string>("LLM_DEPLOYMENT_NAME");
        this.EMBEDDING_DEPLOYMENT_NAME = config.Get<string>("EMBEDDING_DEPLOYMENT_NAME");
        this.LLM_ENDPOINT_URI = config.Get<string>("LLM_ENDPOINT_URI");
        this.LLM_API_KEY = config.Get<string>("LLM_API_KEY");
        this.SEARCH_INDEX = config.Get<string>("SEARCH_INDEX");
        this.SEARCH_ENDPOINT_URI = config.Get<string>("SEARCH_ENDPOINT_URI");
        this.SEARCH_API_KEY = config.Get<string>("SEARCH_API_KEY");
        this.SEARCH_SEMANTIC_CONFIG = config.Get<string>("SEARCH_SEMANTIC_CONFIG").AsString(() => "default");
    }

    public static int GRPC_PORT { get => NetBricks.Config.GetOnce("GRPC_PORT").AsInt(() => 5210); }

    public static int WEB_PORT { get => NetBricks.Config.GetOnce("WEB_PORT").AsInt(() => 5211); }

    public string LLM_DEPLOYMENT_NAME { get; }

    public string EMBEDDING_DEPLOYMENT_NAME { get; }

    public string LLM_ENDPOINT_URI { get; }

    public string LLM_API_KEY { get; }

    public string SEARCH_INDEX { get; }

    public string SEARCH_ENDPOINT_URI { get; }

    public string SEARCH_API_KEY { get; }

    public string SEARCH_SEMANTIC_CONFIG { get; }

    public void Validate()
    {
        this.config.Optional("GRPC_PORT", GRPC_PORT);
        this.config.Optional("WEB_PORT", WEB_PORT);
        this.config.Require("LLM_DEPLOYMENT_NAME", this.LLM_DEPLOYMENT_NAME);
        this.config.Require("EMBEDDING_DEPLOYMENT_NAME", this.EMBEDDING_DEPLOYMENT_NAME);
        this.config.Require("LLM_ENDPOINT_URI", this.LLM_ENDPOINT_URI);
        this.config.Require("LLM_API_KEY", this.LLM_API_KEY, hideValue: true);
        this.config.Require("SEARCH_INDEX", this.SEARCH_INDEX);
        this.config.Require("SEARCH_ENDPOINT_URI", this.SEARCH_ENDPOINT_URI);
        this.config.Require("SEARCH_API_KEY", this.SEARCH_API_KEY, hideValue: true);
        this.config.Require("SEARCH_SEMANTIC_CONFIG", this.SEARCH_SEMANTIC_CONFIG);
    }
}