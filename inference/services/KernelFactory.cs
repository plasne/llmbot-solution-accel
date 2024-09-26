using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using Shared;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Inference;

public class KernelFactory(
    IConfig config,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment webHostEnvironment,
    IServiceProvider serviceProvider)
{
    private readonly IConfig config = config;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IWebHostEnvironment webHostEnvironment = webHostEnvironment;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly SemaphoreSlim semaphore = new(1);
    private readonly Dictionary<int, Kernel> kernelsForEvaluation = [];
    private readonly Dictionary<int, Kernel> kernelsForInference = [];

    private Kernel CreateKernel(HttpClient httpClient, ModelConnectionDetails? embedding, ModelConnectionDetails llm)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder
            .AddAzureOpenAIChatCompletion(
                llm.DeploymentName,
                llm.Endpoint,
                llm.ApiKey,
                httpClient: httpClient);
        if (embedding is not null)
        {
            kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
                embedding.DeploymentName,
                embedding.Endpoint,
                embedding.ApiKey,
                httpClient: httpClient);
        }

        if (!string.IsNullOrEmpty(this.config.OPEN_TELEMETRY_CONNECTION_STRING))
        {
            kernelBuilder.AddOpenTelemetry(this.webHostEnvironment.ApplicationName, this.config.OPEN_TELEMETRY_CONNECTION_STRING);
        }

        var kernel = kernelBuilder.Build();

        foreach (var filter in this.serviceProvider.GetServices<IPromptRenderFilter>())
        {
            kernel.PromptRenderFilters.Add(filter);
        }

        return kernel;
    }

    public async Task<Kernel> GetOrCreateKernelForInferenceAsync(KernelIndex kernelIndex, CancellationToken cancellationToken = default)
    {
        if (this.kernelsForInference.TryGetValue(kernelIndex.Index, out var kernel))
            return kernel;
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (this.kernelsForInference.TryGetValue(kernelIndex.Index, out kernel))
                return kernel;
            var httpClient = this.httpClientFactory.CreateClient("openai-with-retry");
            kernel = this.CreateKernel(httpClient, kernelIndex.EmbeddingConnectionDetails, kernelIndex.LlmConnectionDetails);
            this.kernelsForInference.Add(kernelIndex.Index, kernel);
            return kernel;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task<Kernel> GetOrCreateKernelForEvaluationAsync(KernelIndex kernelIndex, CancellationToken cancellationToken = default)
    {
        if (this.kernelsForEvaluation.TryGetValue(kernelIndex.Index, out var kernel))
            return kernel;
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (this.kernelsForEvaluation.TryGetValue(kernelIndex.Index, out kernel))
                return kernel;
            var httpClient = this.httpClientFactory.CreateClient("openai-without-retry");
            kernel = this.CreateKernel(httpClient, kernelIndex.EmbeddingConnectionDetails, kernelIndex.LlmConnectionDetails);
            this.kernelsForEvaluation.Add(kernelIndex.Index, kernel);
            return kernel;
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}