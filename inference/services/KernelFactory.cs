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

    private Kernel
    CreateKernel(HttpClient httpClient, int llmKernelIndex)
    {
        var kernalBuilder = Kernel.CreateBuilder();

        var llm_endpoint_connection_string = config.LLM_CONNECTION_STRINGS[llmKernelIndex].Split(';');
        var connectionStringParts = llm_endpoint_connection_string.Select(part => part.Split('=')).ToDictionary(split => split[0].Trim(), split => split[1].Trim());
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

    public async Task<Kernel> GetOrCreateKernelForInferenceAsync(int llmKernelIndex, CancellationToken cancellationToken = default)
    {
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
             this.kernelsForInference ??= new List<Kernel>(config.LLM_CONNECTION_STRINGS.Length);
            if (llmKernelIndex >= 0 && llmKernelIndex < this.kernelsForInference.Count)
            {
                return this.kernelsForInference[llmKernelIndex];
            }
            var httpClient = this.httpClientFactory.CreateClient("openai-with-retry");
            this.kernelsForInference.Add(this.CreateKernel(httpClient, llmKernelIndex));
            return this.kernelsForInference[llmKernelIndex];
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task<Kernel> GetOrCreateKernelForEvaluationAsync(int llmKernelIndex, CancellationToken cancellationToken = default)
    {
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            this.kernelsForEvaluation ??= new List<Kernel>(config.LLM_CONNECTION_STRINGS.Length);
            if (llmKernelIndex >= 0 && llmKernelIndex < this.kernelsForEvaluation.Count)
            {
                return this.kernelsForEvaluation[llmKernelIndex];
            }
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