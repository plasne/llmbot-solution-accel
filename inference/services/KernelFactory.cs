using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
    private Kernel? kernelForInference;
    private Kernel? kernelForEvaluation;

    private Kernel CreateKernel(HttpClient httpClient)
    {
        var kernalBuilder = Kernel.CreateBuilder();
        kernalBuilder
            .AddAzureOpenAIChatCompletion(
                config.LLM_DEPLOYMENT_NAME,
                config.LLM_ENDPOINT_URI,
                config.LLM_API_KEY,
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

    public async Task<Kernel> GetOrCreateKernelForInferenceAsync(CancellationToken cancellationToken = default)
    {
        if (this.kernelForInference is not null) return this.kernelForInference;

        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (this.kernelForInference is not null) return this.kernelForInference;
            var httpClient = this.httpClientFactory.CreateClient("openai-with-retry");
            this.kernelForInference = this.CreateKernel(httpClient);
            return this.kernelForInference;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task<Kernel> GetOrCreateKernelForEvaluationAsync(CancellationToken cancellationToken = default)
    {
        if (this.kernelForEvaluation is not null) return this.kernelForEvaluation;

        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (this.kernelForEvaluation is not null) return this.kernelForEvaluation;
            var httpClient = this.httpClientFactory.CreateClient("openai-without-retry");
            this.kernelForEvaluation = this.CreateKernel(httpClient);
            return this.kernelForEvaluation;
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}