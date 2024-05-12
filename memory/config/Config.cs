using Iso8601DurationHelper;
using NetBricks;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.PORT = this.config.Get<string>("PORT").AsInt(() => 7010);
        this.OPEN_TELEMETRY_CONNECTION_STRING = this.config.GetSecret<string>("OPEN_TELEMETRY_CONNECTION_STRING").Result;
        this.SQL_SERVER_MAX_RETRY_ATTEMPTS = this.config.Get<string>("SQL_SERVER_MAX_RETRY_ATTEMPTS").AsInt(() => 3);
        this.SQL_SERVER_SECONDS_BETWEEN_RETRIES = this.config.Get<string>("SQL_SERVER_SECONDS_BETWEEN_RETRIES").AsInt(() => 2);
        this.SQL_SERVER_HISTORY_SERVICE_CONNSTRING = this.config.GetSecret<string>("SQL_SERVER_HISTORY_SERVICE_CONNSTRING").Result;
        this.DEFAULT_RETENTION = this.config.Get<string>("DEFAULT_RETENTION").AsDuration(() => Duration.FromMonths(3));
    }

    public int PORT { get; }

    public string OPEN_TELEMETRY_CONNECTION_STRING { get; }

    public int SQL_SERVER_MAX_RETRY_ATTEMPTS { get; }

    public int SQL_SERVER_SECONDS_BETWEEN_RETRIES { get; }

    public string SQL_SERVER_HISTORY_SERVICE_CONNSTRING { get; }

    public Duration DEFAULT_RETENTION { get; }

    public void Validate()
    {
        this.config.Require("PORT", this.PORT);
        this.config.Require("OPEN_TELEMETRY_CONNECTION_STRING", OPEN_TELEMETRY_CONNECTION_STRING, hideValue: true);
        this.config.Optional("SQL_SERVER_MAX_RETRY_ATTEMPTS", this.SQL_SERVER_MAX_RETRY_ATTEMPTS);
        this.config.Optional("SQL_SERVER_SECONDS_BETWEEN_RETRIES", this.SQL_SERVER_SECONDS_BETWEEN_RETRIES);
        this.config.Optional("SQL_SERVER_HISTORY_SERVICE_CONNSTRING", this.SQL_SERVER_HISTORY_SERVICE_CONNSTRING, hideValue: true);
        this.config.Optional("DEFAULT_RETENTION", this.DEFAULT_RETENTION.ToString());
    }
}