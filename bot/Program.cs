using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Bots;
using Channels;
using NetBricks;
using dotenv.net;
using Shared;

// create a new web app builder
var builder = WebApplication.CreateBuilder(args);

// add config
DotEnv.Load();
var netConfig = new NetBricks.Config();
var config = new Config(netConfig);
config.Validate();
builder.Services.AddSingleton<IConfig>(config);

// add logging
builder.Logging.ClearProviders();
builder.Services.AddSingleLineConsoleLogger();
builder.Logging.AddOpenTelemetry(config.OPEN_TELEMETRY_CONNECTION_STRING);
builder.Services.AddOpenTelemetry(DiagnosticService.Source.Name, builder.Environment.ApplicationName, config.OPEN_TELEMETRY_CONNECTION_STRING);

// add basic services
builder.Services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
});

// add the services required for communicating with the bot
builder.Services.AddSingleton<HistoryService>();
builder.Services.AddSingleton<BotChannel>();

// add bot framework authentication
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// add error handling
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// create the bot as transient
builder.Services.AddTransient<IBot, ChatBot>();

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(config.PORT);
});

// build the app
var app = builder.Build();

// define the app's routes
app.UseDefaultFiles()
    .UseWebSockets()
    .UseRouting()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });

// run the app
app.Run();