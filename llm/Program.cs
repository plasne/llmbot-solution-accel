using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NetBricks;

var builder = WebApplication.CreateBuilder(args);

// add config
builder.Services.AddConfig();
builder.Services.AddSingleton<IConfig, Config>();
builder.Services.AddHostedService<LifecycleService>();

// add other services
builder.Services.AddGrpc();
builder.Services.AddSingleton<WeatherForecaster>();

// disable TLS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5210, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<WeatherService>();

app.Run();