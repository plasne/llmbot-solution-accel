using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using NetBricks;
using dotenv.net;
using Microsoft.Extensions.Logging;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// add logging
builder.Logging.ClearProviders();
builder.Services.AddSingleLineConsoleLogger();

// add config
builder.Services.AddConfig();
builder.Services.AddSingleton<IConfig, Config>();
builder.Services.AddHostedService<LifecycleService>();

// add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// add the kernel service
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfig>()!;

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

// add the workflow services
builder.Services.AddScoped<IContext, Context>();
builder.Services.AddSingleton<IMemory, VolatileMemory>();
builder.Services.AddTransient<Workflow>();
builder.Services.AddTransient<DetermineIntent>();
builder.Services.AddTransient<GetDocuments>();
builder.Services.AddTransient<SelectGroundingData>();
builder.Services.AddTransient<GenerateAnswer>();

// add other services
builder.Services.AddGrpc();
builder.Services.AddControllers();
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
app.MapGrpcService<ChatService>();
app.MapControllers();

app.Run();