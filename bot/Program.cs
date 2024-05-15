using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using NetBricks;
using dotenv.net;
using Shared;
using System;
using Polly;
using Polly.Extensions.Http;
using Bot;

DotEnv.Load();

// create a new web app builder
var builder = WebApplication.CreateBuilder(args);

// add config
var netConfig = new NetBricks.Config();
await netConfig.Apply();
var config = new Bot.Config(netConfig);
config.Validate();
builder.Services.AddSingleton<Bot.IConfig>(config);

// add logging
builder.Logging.ClearProviders();
builder.Services.AddSingleLineConsoleLogger();
builder.Logging.AddOpenTelemetry(config.OPEN_TELEMETRY_CONNECTION_STRING);
builder.Services.AddOpenTelemetry(DiagnosticService.Source.Name, builder.Environment.ApplicationName, config.OPEN_TELEMETRY_CONNECTION_STRING);

// add http client with retry
builder.Services
    .AddHttpClient("retry", options =>
    {
        options.Timeout = TimeSpan.FromSeconds(config.MAX_TIMEOUT_IN_SECONDS);
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(config.MAX_RETRY_ATTEMPTS, retryAttempt => TimeSpan.FromSeconds(config.SECONDS_BETWEEN_RETRIES)));

// add controllers
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
});

// add the services required for communicating with the bot
builder.Services.AddSingleton<ICardProvider, InMemoryCardProvider>();
builder.Services.AddSingleton<BotChannel>();

// add bot framework authentication
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// add error handling
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// create the bot as transient
builder.Services.AddTransient<IBot, ChatBot>();

// add commands
builder.Services.AddTransient<ICommands, HelpCommand>();
builder.Services.AddTransient<ICommands, FeedbackCommands>();
builder.Services.AddTransient<ICommands, MemoryCommands>();

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(config.PORT);
});
builder.Services.AddHttpContextAccessor();

// build the app
var app = builder.Build();
app.Use(async (ctx, req) =>
{
    ctx.Items[ChatBot.StartTimeKey] = DateTime.UtcNow;
    await req.Invoke();
});

// define the app's routes
app.UseWebSockets()
    .UseRouting()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });

// run the app
app.Run();