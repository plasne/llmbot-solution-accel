using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using Shared;

namespace Inference;

public class KernelFactory(IConfig config, IHttpClientFactory httpClientFactory, IWebHostEnvironment webHostEnvironment)
{
    private readonly IConfig config = config;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IWebHostEnvironment webHostEnvironment = webHostEnvironment;
    private readonly SemaphoreSlim semaphore = new(1);
    private List<Kernel>? kernelsForEvaluation;
    private List<Kernel>? kernelsForInference;
    private int llmKernelIndex = 0;

    private Kernel
    CreateKernel(HttpClient httpClient, int llmKernelIndex)
    {
        var kernalBuilder = Kernel.CreateBuilder();

        var llm_endpont_connection_string = config.CHAT_LLM_CONNECTION_STRINGS[llmKernelIndex].Split(';');
        var connectionStringParts = llm_endpont_connection_string.Select(part => part.Split('=')).ToDictionary(split => split[0], split => split[1]);
        var llm_deployment_name = connectionStringParts["DeploymentName"];
        var llm_endpoint_uri = connectionStringParts["Endpoint"];
        var llm_api_key = connectionStringParts["ApiKey"];

        kernalBuilder
            .AddAzureOpenAIChatCompletion(
                llm_deployment_name,
                llm_endpoint_uri,
                llm_api_key,
                httpClient: httpClient)
            .AddAzureOpenAITextEmbeddingGeneration(
                config.EMBEDDING_DEPLOYMENT_NAME,
                config.EMBEDDING_ENDPOINT_URI,
                config.EMBEDDING_API_KEY,
                httpClient: httpClient);

        if (!string.IsNullOrEmpty(this.config.OPEN_TELEMETRY_CONNECTION_STRING))
        {
            kernalBuilder.AddOpenTelemetry(this.webHostEnvironment.ApplicationName, this.config.OPEN_TELEMETRY_CONNECTION_STRING);
        }

        return kernalBuilder.Build();
    }

    public async Task<Kernel> GetOrCreateKernelForInferenceAsync(CancellationToken cancellationToken = default, int? index = null)
    {
        this.kernelsForInference ??= new List<Kernel>(config.CHAT_LLM_CONNECTION_STRINGS.Length);
        this.llmKernelIndex = index ?? this.llmKernelIndex;

        if (this.kernelsForInference[llmKernelIndex] is not null) return this.kernelsForInference[llmKernelIndex];

        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (this.kernelsForInference[llmKernelIndex] is not null) return this.kernelsForInference[llmKernelIndex];
            var httpClient = this.httpClientFactory.CreateClient("openai-with-retry");
            this.kernelsForInference.Add(this.CreateKernel(httpClient, llmKernelIndex));
            return this.kernelsForInference[llmKernelIndex];
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task<Kernel> GetOrCreateKernelForEvaluationAsync(CancellationToken cancellationToken = default, int? index = null)
    {
        this.kernelsForEvaluation ??= new List<Kernel>(config.CHAT_LLM_CONNECTION_STRINGS.Length);
        this.llmKernelIndex = index ?? this.llmKernelIndex;
        if (this.kernelsForEvaluation[llmKernelIndex] is not null) return this.kernelsForEvaluation[llmKernelIndex];

        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (this.kernelsForEvaluation[llmKernelIndex] is not null) return this.kernelsForEvaluation[llmKernelIndex];
            var httpClient = this.httpClientFactory.CreateClient("openai-without-retry");
            this.kernelsForEvaluation.Add(this.CreateKernel(httpClient, llmKernelIndex));
            return this.kernelsForEvaluation[llmKernelIndex];
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}