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
    private readonly Dictionary<int, Kernel> kernelsForEvaluation = [];
    private readonly Dictionary<int, Kernel> kernelsForInference = [];

    private Kernel
    CreateKernel(HttpClient httpClient, int index)
    {
        var kernalBuilder = Kernel.CreateBuilder();
        var details = this.config.LLM_CONNECTION_STRINGS[index];

        kernalBuilder
            .AddAzureOpenAIChatCompletion(
                details.DeploymentName,
                details.Endpoint,
                details.ApiKey,
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

    public async Task<Kernel> GetOrCreateKernelForInferenceAsync(int index, CancellationToken cancellationToken = default)
    {
        if (this.kernelsForInference.TryGetValue(index, out var kernel)) return kernel;
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (this.kernelsForInference.TryGetValue(index, out kernel)) return kernel;
            var httpClient = this.httpClientFactory.CreateClient("openai-with-retry");
            kernel = this.CreateKernel(httpClient, index);
            this.kernelsForInference.Add(index, kernel);
            return kernel;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task<Kernel> GetOrCreateKernelForEvaluationAsync(int index, CancellationToken cancellationToken = default)
    {
        if (this.kernelsForEvaluation.TryGetValue(index, out var kernel)) return kernel;
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (this.kernelsForEvaluation.TryGetValue(index, out kernel)) return kernel;
            var httpClient = this.httpClientFactory.CreateClient("openai-without-retry");
            kernel = this.CreateKernel(httpClient, index);
            this.kernelsForEvaluation.Add(index, kernel);
            return kernel;
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}