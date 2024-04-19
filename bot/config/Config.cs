using NetBricks;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.PORT = this.config.Get<string>("PORT").AsInt(() => 3978);
        this.LLM_URI = this.config.Get<string>("LLM_URI").AsString(() => "http://localhost:5210");
        this.CHARACTERS_PER_UPDATE = this.config.Get<string>("CHARACTERS_PER_UPDATE").AsInt(() => 200);
    }

    public int PORT { get; }

    public string LLM_URI { get; }

    public int CHARACTERS_PER_UPDATE { get; }

    public void Validate()
    {
        this.config.Require("PORT", PORT);
        this.config.Require("LLM_URI", this.LLM_URI);
        this.config.Require("CHARACTERS_PER_UPDATE", this.CHARACTERS_PER_UPDATE);
        this.config.Require("MicrosoftAppType");
        this.config.Require("MicrosoftAppId");
        this.config.Require("MicrosoftAppPassword", hideValue: true);
    }
}