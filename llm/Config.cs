using System;
using dotenv.net;
using Microsoft.Extensions.Logging;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;
    private readonly ILogger<Config> logger;

    public Config(NetBricks.IConfig config, ILogger<Config> logger)
    {
        this.config = config;
        this.logger = logger;

        DotEnv.Load();
        this.LLM_DEPLOYMENT_NAME = config.Get<string>("LLM_DEPLOYMENT_NAME");
        this.LLM_ENDPOINT_URI = config.Get<string>("LLM_ENDPOINT_URI");
        this.LLM_API_KEY = config.Get<string>("LLM_API_KEY");
    }

    public string LLM_DEPLOYMENT_NAME { get; }

    public string LLM_ENDPOINT_URI { get; }

    public string LLM_API_KEY { get; }

    public void Validate()
    {
        this.config.Require("LLM_DEPLOYMENT_NAME");
        this.config.Require("LLM_ENDPOINT_URI");
        this.config.Require("LLM_API_KEY", hideValue: true);
    }
}