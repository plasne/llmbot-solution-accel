using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetBricks;
using dotenv.net;
using Microsoft.Extensions.Logging;
using Shared;
using System;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// add config
var netConfig = new NetBricks.Config();
await netConfig.Apply();
var config = new Config(netConfig);
config.Validate();
builder.Services.AddSingleton<IConfig>(config);
builder.Services.AddSingleton<NetBricks.IConfig>(netConfig);
builder.Services.AddDefaultAzureCredential();

// add logging
builder.Logging.ClearProviders();
builder.Services.AddSingleLineConsoleLogger();
builder.Logging.AddOpenTelemetry(config.OPEN_TELEMETRY_CONNECTION_STRING);
builder.Services.AddOpenTelemetry(DiagnosticService.Source.Name, builder.Environment.ApplicationName, config.OPEN_TELEMETRY_CONNECTION_STRING);

// add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen().AddSwaggerGenNewtonsoftSupport();

// add other services
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
});

// add the appropriate history service
if (!string.IsNullOrEmpty(config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING))
{
    Console.WriteLine("ADDING SERVICE: SqlServerHistoryService");
    builder.Services.AddSingleton<IMemoryStore, SqlServerMemoryStore>();
}
else
{
    Console.WriteLine("ADDING SERVICE: LocalMemoryHistoryService");
    builder.Services.AddSingleton<IMemoryStore, LocalMemoryStore>();
}

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(config.PORT);
});

var app = builder.Build();

// use swagger
app.UseSwagger();
app.UseSwaggerUI();

// use routing, gRPC, and controllers
app.UseRouting();
app.UseMiddleware<HttpExceptionMiddleware>();
app.MapControllers();

app.Run();