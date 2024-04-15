using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NetBricks;
using dotenv.net;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// add config
builder.Services.AddConfig();
builder.Services.AddSingleton<IConfig, Config>();
builder.Services.AddHostedService<LifecycleService>();

// add other services
builder.Services.AddGrpc();
builder.Services.AddSingleton<WeatherForecaster>();

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(Config.PORT, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<WeatherService>();

app.Run();