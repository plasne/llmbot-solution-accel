using NetBricks;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.PORT = this.config.Get<string>("PORT").AsInt(() => 7000);
        this.OPEN_TELEMETRY_CONNECTION_STRING = this.config.GetSecret<string>("OPEN_TELEMETRY_CONNECTION_STRING").Result;
        this.LLM_URI = this.config.Get<string>("LLM_URI").AsString(() => "http://localhost:5210");
        this.CHARACTERS_PER_UPDATE = this.config.Get<string>("CHARACTERS_PER_UPDATE").AsInt(() => 200);
        this.FINAL_STATUS = this.config.Get<string>("FINAL_STATUS").AsString(() => "Generated.");
        this.MEMORY_URL = this.config.Get<string>("MEMORY_URL").AsString(() => "http://localhost:7010");
    }

    public int PORT { get; }

    public string OPEN_TELEMETRY_CONNECTION_STRING { get; }

    public string LLM_URI { get; }

    public int CHARACTERS_PER_UPDATE { get; }

    public string FINAL_STATUS { get; }

    public string MEMORY_URL { get; }

    public void Validate()
    {
        this.config.Require("PORT", this.PORT);
        this.config.Require("OPEN_TELEMETRY_CONNECTION_STRING", OPEN_TELEMETRY_CONNECTION_STRING, hideValue: true);
        this.config.Require("LLM_URI", this.LLM_URI);
        this.config.Require("CHARACTERS_PER_UPDATE", this.CHARACTERS_PER_UPDATE);
        this.config.Require("FINAL_STATUS", this.FINAL_STATUS);
        this.config.Require("MEMORY_URL", this.MEMORY_URL);
        this.config.Require("MicrosoftAppType");
        this.config.Require("MicrosoftAppId");
        this.config.Require("MicrosoftAppPassword", hideValue: true);
    }
}