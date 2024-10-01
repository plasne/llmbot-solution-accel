using System.Runtime.InteropServices;
using ChangeFeed;
using NetBricks;

namespace Bot;

public class Config : IConfig, IEventHubChangeFeedConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.PORT = this.config.Get<string>("PORT").AsInt(() => 7000);
        this.OPEN_TELEMETRY_CONNECTION_STRING = this.config.GetSecret<string>("OPEN_TELEMETRY_CONNECTION_STRING").Result;
        this.MEMORY_URL = this.config.Get<string>("MEMORY_URL").AsString(() => "http://localhost:7010");
        this.INFERENCE_URL = this.config.Get<string>("INFERENCE_URL").AsString(() => "http://localhost:7020");
        this.CHARACTERS_PER_UPDATE = this.config.Get<string>("CHARACTERS_PER_UPDATE").AsInt(() => 200);
        this.FINAL_STATUS = this.config.Get<string>("FINAL_STATUS").AsString(() => "Generated.");
        this.MAX_RETRY_ATTEMPTS = this.config.Get<string>("MAX_RETRY_ATTEMPTS").AsInt(() => 3);
        this.SECONDS_BETWEEN_RETRIES = this.config.Get<string>("SECONDS_BETWEEN_RETRIES").AsInt(() => 2);
        this.MAX_TIMEOUT_IN_SECONDS = this.config.Get<string>("MAX_TIMEOUT_IN_SECONDS").AsInt(() => 60);
        this.VALID_TENANTS = this.config.Get<string>("VALID_TENANTS").AsArray(() => []);
        this.MAX_PAYLOAD_SIZE = this.config.Get<string>("MAX_PAYLOAD_SIZE").AsInt(() => 36864);
        this.CHANGEFEED_CONNSTRING = this.config.GetSecret<string>("CHANGEFEED_CONNSTRING").Result;
        this.CHANGEFEED_CONSUMER_GROUPS = this.config.Get<string>("CHANGEFEED_CONSUMER_GROUPS").AsArray(() => []);
    }

    public int PORT { get; }

    public string OPEN_TELEMETRY_CONNECTION_STRING { get; }

    public string MEMORY_URL { get; }

    public string INFERENCE_URL { get; }

    public int CHARACTERS_PER_UPDATE { get; }

    public string FINAL_STATUS { get; }

    public int MAX_RETRY_ATTEMPTS { get; }

    public int SECONDS_BETWEEN_RETRIES { get; }

    public int MAX_TIMEOUT_IN_SECONDS { get; }

    public int MAX_PAYLOAD_SIZE { get; }

    public string[] VALID_TENANTS { get; }

    public string CHANGEFEED_CONNSTRING { get; }

    public string[] CHANGEFEED_CONSUMER_GROUPS { get; }

    public void Validate()
    {
        this.config.Require("PORT", this.PORT);
        this.config.Optional("OPEN_TELEMETRY_CONNECTION_STRING", OPEN_TELEMETRY_CONNECTION_STRING, hideValue: true);
        this.config.Require("MEMORY_URL", this.MEMORY_URL);
        this.config.Require("INFERENCE_URL", this.INFERENCE_URL);
        this.config.Require("CHARACTERS_PER_UPDATE", this.CHARACTERS_PER_UPDATE);
        this.config.Require("FINAL_STATUS", this.FINAL_STATUS);
        this.config.Require("MAX_RETRY_ATTEMPTS", this.MAX_RETRY_ATTEMPTS);
        this.config.Require("SECONDS_BETWEEN_RETRIES", this.SECONDS_BETWEEN_RETRIES);
        this.config.Require("MAX_TIMEOUT_IN_SECONDS", this.MAX_TIMEOUT_IN_SECONDS);
        this.config.Require("MAX_PAYLOAD_SIZE", this.MAX_PAYLOAD_SIZE);
        this.config.Optional("VALID_TENANTS", this.VALID_TENANTS);
        this.config.Require("MicrosoftAppType");
        this.config.Require("MicrosoftAppId");
        this.config.Require("MicrosoftAppPassword", hideValue: true);
        this.config.Optional("CHANGEFEED_CONNSTRING", this.CHANGEFEED_CONNSTRING, hideValue: true);
        this.config.Optional("CHANGEFEED_CONSUMER_GROUPS", this.CHANGEFEED_CONSUMER_GROUPS);
    }
}
