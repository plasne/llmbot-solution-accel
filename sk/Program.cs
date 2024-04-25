using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using NetBricks;
using dotenv.net;
using Microsoft.Extensions.Logging;
using Shared;
using System;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// add config
var netConfig = new NetBricks.Config();
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

// add the inference pipeline service
if (!string.IsNullOrEmpty(config.AZURE_STORAGE_ACCOUNT_NAME)
    && !string.IsNullOrEmpty(config.AZURE_STORAGE_INFERENCE_QUEUE)
    && !string.IsNullOrEmpty(config.AZURE_STORAGE_EVALUATION_QUEUE))
{
    Console.WriteLine("ADDING SERVICE: InferencePipelineService");
    builder.Services.AddHttpClient();
    builder.Services.AddHostedService<InferencePipelineService>();
}

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

    kernalBuilder.AddOpenTelemetry(builder.Environment.ApplicationName, config.OPEN_TELEMETRY_CONNECTION_STRING);
    return kernalBuilder.Build();
});

// register memory provider
switch (config.MEMORY_TERM)
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
builder.Services.AddTransient<ApplyIntent>();
builder.Services.AddTransient<GetDocuments>();
builder.Services.AddTransient<SelectGroundingData>();
builder.Services.AddTransient<GenerateAnswer>();

// add other services
builder.Services.AddGrpc();
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
});
builder.Services.AddSingleton<SearchService>();

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(config.GRPC_PORT, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
    options.ListenAnyIP(config.WEB_PORT, listenOptions =>
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