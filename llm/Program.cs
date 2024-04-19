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

// add logging
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(DiagnosticService.Source.Name, Config.OPEN_TELEMETRY_CONNECTION_STRING);
builder.Services.AddSingleLineConsoleLogger();
builder.Services.AddOpenTelemetry(DiagnosticService.Source.Name, builder.Environment.ApplicationName, Config.OPEN_TELEMETRY_CONNECTION_STRING);

// add config
builder.Services.AddConfig();
builder.Services.AddSingleton<IConfig, Config>();
builder.Services.AddHostedService<LifecycleService>();

// add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen().AddSwaggerGenNewtonsoftSupport();

// add the kernel service
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfig>()!;

    var kernalBuilder = Kernel.CreateBuilder();
    kernalBuilder.Services
        .AddAzureOpenAIChatCompletion(
            config.LLM_DEPLOYMENT_NAME,
            config.LLM_ENDPOINT_URI,
            config.LLM_API_KEY)
        .AddAzureOpenAITextEmbeddingGeneration(
            config.EMBEDDING_DEPLOYMENT_NAME,
            config.LLM_ENDPOINT_URI,
            config.LLM_API_KEY);

    kernalBuilder.AddOpenTelemetry(builder.Environment.ApplicationName, Config.OPEN_TELEMETRY_CONNECTION_STRING);
    return kernalBuilder.Build();
});

// register memory provider
switch (Config.MEMORY_TERM)
{
    case MemoryTerm.Long:
        builder.Services.AddSingleton<IMemory, UnsafeMemory>();
        break;
    case MemoryTerm.Short:
        builder.Services.AddScoped<IMemory, UnsafeMemory>();
        break;
}

// add the workflow services
builder.Services.AddScoped<IContext, Context>();
builder.Services.AddTransient<Workflow>();
builder.Services.AddTransient<DetermineIntent>();
builder.Services.AddTransient<GetDocuments>();
builder.Services.AddTransient<SelectGroundingData>();
builder.Services.AddTransient<GenerateAnswer>();

// add other services
builder.Services.AddGrpc();
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSingleton<SearchService>();

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(Config.GRPC_PORT, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
    options.ListenAnyIP(Config.WEB_PORT, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
});

var app = builder.Build();

// use swagger
app.UseSwagger();
app.UseSwaggerUI();

// use routing, gRPC, and controllers
app.UseRouting();
app.UseMiddleware<HttpExceptionMiddleware>();
app.MapGrpcService<ChatService>();
app.MapControllers();

app.Run();