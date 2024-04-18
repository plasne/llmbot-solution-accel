using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using NetBricks;
using dotenv.net;
using Microsoft.Extensions.Logging;
using Shared;
using llm;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

// add config
//builder.Services.AddSingleLineConsoleLogger();
builder.AddOpenTelemetry(DiagnosticService.Source.Name, Config.OpenTelemetryConnectionString);
builder.Services.AddConfig();
builder.Services.AddSingleton<IConfig, Config>();
builder.Services.AddHostedService<LifecycleService>();

// add the kernel service
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetService<IConfig>()!;

    var builder = Kernel.CreateBuilder();
    builder.Services
        .AddAzureOpenAIChatCompletion(
            config.LLM_DEPLOYMENT_NAME,
            config.LLM_ENDPOINT_URI,
            config.LLM_API_KEY)
        .AddAzureOpenAITextEmbeddingGeneration(
            config.EMBEDDING_DEPLOYMENT_NAME,
            config.LLM_ENDPOINT_URI,
            config.LLM_API_KEY);
    return builder.Build();
});

// add other services
builder.Services.AddGrpc();
builder.Services.AddSingleton<SearchService>();

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(Config.PORT, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<ChatService>();

app.Run();