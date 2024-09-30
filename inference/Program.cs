using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NetBricks;
using dotenv.net;
using Microsoft.Extensions.Logging;
using Shared;
using System;
using Polly.Extensions.Http;
using Polly;
using Inference;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Hosting;
using System.Linq;

// load environment variables
var ENV_FILES = NetBricks.Config.GetOnce("ENV_FILES").AsArray(() => ["local.env"]);
DotEnv.Load(new DotEnvOptions(envFilePaths: ENV_FILES, overwriteExistingVars: false));

// create a new web app builder
var builder = WebApplication.CreateBuilder(args);

// add config
var netConfig = new NetBricks.Config();
await netConfig.Apply();
var config = new Inference.Config(netConfig);
config.Validate();
builder.Services.AddSingleton<Inference.IConfig>(config);
builder.Services.AddSingleton<NetBricks.IConfig>(netConfig);
builder.Services.AddDefaultAzureCredential();

// add logging
builder.Logging.ClearProviders();
builder.Services.AddSingleLineConsoleLogger();
if (!string.IsNullOrEmpty(config.OPEN_TELEMETRY_CONNECTION_STRING))
{
    builder.Logging.AddOpenTelemetry(config.OPEN_TELEMETRY_CONNECTION_STRING);
    builder.Services.AddOpenTelemetry(DiagnosticService.Source.Name, builder.Environment.ApplicationName, config.OPEN_TELEMETRY_CONNECTION_STRING);
}

// add http clients (not OpenAI)
builder.Services
    .AddHttpClient("retry", options =>
    {
        options.Timeout = TimeSpan.FromSeconds(config.MAX_TIMEOUT_IN_SECONDS);
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(config.MAX_RETRY_ATTEMPTS, retryAttempt => TimeSpan.FromSeconds(config.SECONDS_BETWEEN_RETRIES)));

// add http clients (OpenAI)
builder.Services
    .AddHttpClient("openai-with-retry", options =>
    {
        options.Timeout = TimeSpan.FromSeconds(config.MAX_TIMEOUT_IN_SECONDS);
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(config.MAX_RETRY_ATTEMPTS, retryAttempt => TimeSpan.FromSeconds(config.SECONDS_BETWEEN_RETRIES)));

// add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen().AddSwaggerGenNewtonsoftSupport();

// add the kernel factory service
builder.Services.AddSingleton<KernelFactory>();

// register memory provider
switch (config.MEMORY_TERM)
{
    case MemoryTerm.Long:
        builder.Services.AddSingleton<IMemory, SafeMemory>();
        break;
    case MemoryTerm.Short:
        builder.Services.AddScoped<IMemory, SafeMemory>();
        break;
}

// add the workflow services
builder.Services.AddScoped<IWorkflowContext, WorkflowContext>();
builder.Services.AddTransient<PrimaryWorkflow>();
builder.Services.AddTransient<InDomainOnlyWorkflow>();
builder.Services.AddTransient<PickDocumentsWorkflow>();

// add the workflow steps
Console.WriteLine("Workflow Step Configuration:");
if (config.LLM_CONNECTION_STRINGS.Any())
{
    Console.WriteLine("- IDetermineIntent will use DetermineIntentWithLlm.");
    builder.Services.AddTransient<IDetermineIntent, DetermineIntentWithLlm>();
}
else
{
    Console.WriteLine("- IDetermineIntent will use InDomainOnlyIntent.");
    builder.Services.AddTransient<IDetermineIntent, InDomainOnlyIntent>();
}
builder.Services.AddTransient<InDomainOnlyIntent>();
Console.WriteLine("- ApplyIntent will use ApplyIntent.");
builder.Services.AddTransient<ApplyIntent>();
if (!string.IsNullOrEmpty(config.SEARCH_ENDPOINT_URI))
{
    Console.WriteLine("- IGetDocuments will use GetDocumentsFromAzureAISearch.");
    Console.WriteLine("- IPickDocuments will use PickDocumentsFromAzureAISearch.");
    builder.Services.AddTransient<IGetDocuments, GetDocumentsFromAzureAISearch>();
    builder.Services.AddTransient<IPickDocuments, PickDocumentsFromAzureAISearch>();
}
else
{
    Console.WriteLine("- IGetDocuments will use GetDocumentsFromBicyleShop.");
    Console.WriteLine("- IPickDocuments will use PickDocumentsFromBicycleShop.");
    builder.Services.AddTransient<IGetDocuments, GetDocumentsFromBicyleShop>();
    builder.Services.AddTransient<IPickDocuments, PickDocumentsFromBicycleShop>();
}
Console.WriteLine("- SortDocuments will use SortDocuments.");
builder.Services.AddTransient<SortDocuments>();
Console.WriteLine("- SelectGroundingData will use SelectGroundingData.");
builder.Services.AddTransient<SelectGroundingData>();
if (config.LLM_CONNECTION_STRINGS.Any())
{
    Console.WriteLine("- IGenerateAnswer will use GenerateAnswerWithLlm.");
    builder.Services.AddTransient<IGenerateAnswer, GenerateAnswerWithLlm>();
}
else
{
    Console.WriteLine("- IGenerateAnswer will use AnswerWithCitationsOnly.");
    builder.Services.AddTransient<IGenerateAnswer, AnswerWithCitationsOnly>();
}

// add filters
builder.Services.AddSingleton<IPromptRenderFilter, PromptTokenCountFilter>();

// add other services
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddControllers().AddNewtonsoftJson();

// listen (disable TLS)
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(config.GRPC_PORT, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
    options.ListenLocalhost(config.WEB_PORT, listenOptions =>
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
app.MapGrpcReflectionService();

app.MapControllers();

// run
await app.RunAsync();