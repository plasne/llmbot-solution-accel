using NetBricks;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.LLM_URI = this.config.Get<string>("LLM_URI").AsString(() => "http://localhost:5210");
        this.CHARACTERS_PER_UPDATE = this.config.Get<string>("CHARACTERS_PER_UPDATE").AsInt(() => 200);
    }

    public static int PORT { get => NetBricks.Config.GetOnce("PORT").AsInt(() => 3978); }

    public static string OPEN_TELEMETRY_CONNECTION_STRING { get => NetBricks.Config.GetOnce("OPEN_TELEMETRY_CONNECTION_STRING"); }

    public string LLM_URI { get; }

    public int CHARACTERS_PER_UPDATE { get; }

    public void Validate()
    {
        this.config.Optional("PORT", PORT);
        this.config.Require("LLM_URI", this.LLM_URI);
        this.config.Require("MicrosoftAppType");
        this.config.Require("MicrosoftAppId");
        this.config.Require("MicrosoftAppPassword", hideValue: true);
        this.config.Require("OPEN_TELEMETRY_CONNECTION_STRING", OPEN_TELEMETRY_CONNECTION_STRING);
    }
}