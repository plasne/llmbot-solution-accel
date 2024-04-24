using NetBricks;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.PORT = this.config.Get<string>("PORT").AsInt(() => 3978);
        this.OPEN_TELEMETRY_CONNECTION_STRING = this.config.Get<string>("OPEN_TELEMETRY_CONNECTION_STRING");
        this.LLM_URI = this.config.Get<string>("LLM_URI").AsString(() => "http://localhost:5210");
        this.CHARACTERS_PER_UPDATE = this.config.Get<string>("CHARACTERS_PER_UPDATE").AsInt(() => 200);
        this.FINAL_STATUS = this.config.Get<string>("FINAL_STATUS").AsString(() => "Generated.");
    }

    public int PORT { get; }

    public string OPEN_TELEMETRY_CONNECTION_STRING { get; }

    public string LLM_URI { get; }

    public int CHARACTERS_PER_UPDATE { get; }

    public string FINAL_STATUS { get; }

    public void Validate()
    {
        this.config.Require("PORT", PORT);
        this.config.Require("OPEN_TELEMETRY_CONNECTION_STRING", OPEN_TELEMETRY_CONNECTION_STRING);
        this.config.Require("LLM_URI", this.LLM_URI);
        this.config.Require("CHARACTERS_PER_UPDATE", this.CHARACTERS_PER_UPDATE);
        this.config.Require("FINAL_STATUS", this.FINAL_STATUS);
        this.config.Require("MicrosoftAppType");
        this.config.Require("MicrosoftAppId");
        this.config.Require("MicrosoftAppPassword", hideValue: true);
    }
}