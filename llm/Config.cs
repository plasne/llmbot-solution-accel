using NetBricks;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.LLM_DEPLOYMENT_NAME = config.Get<string>("LLM_DEPLOYMENT_NAME");
        this.LLM_ENDPOINT_URI = config.Get<string>("LLM_ENDPOINT_URI");
        this.LLM_API_KEY = config.Get<string>("LLM_API_KEY");
    }

    public static int PORT { get => NetBricks.Config.GetOnce("PORT").AsInt(() => 5210); }

    public string LLM_DEPLOYMENT_NAME { get; }

    public string LLM_ENDPOINT_URI { get; }

    public string LLM_API_KEY { get; }

    public void Validate()
    {
        this.config.Optional("PORT", PORT);
        this.config.Require("LLM_DEPLOYMENT_NAME", this.LLM_DEPLOYMENT_NAME);
        this.config.Require("LLM_ENDPOINT_URI", this.LLM_ENDPOINT_URI);
        this.config.Require("LLM_API_KEY", this.LLM_API_KEY, hideValue: true);
    }
}