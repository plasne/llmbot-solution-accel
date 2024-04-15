using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Bots;
using Weather;
using NetBricks;
using dotenv.net;

DotEnv.Load();

// create a new web app builder
var builder = WebApplication.CreateBuilder(args);

// add logging
builder.Logging.AddDebug();
builder.Logging.AddConsole();

// add config
builder.Services.AddConfig();
builder.Services.AddSingleton<IConfig, Config>();
builder.Services.AddHostedService<LifecycleService>();

// add basic services
builder.Services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
});

// add the weather channel
builder.Services.AddSingleton<WeatherChannel>();

// add bot framework authentication
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// add error handling
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// create the bot as transient
builder.Services.AddTransient<IBot, WeatherBot>();

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(Config.PORT);
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