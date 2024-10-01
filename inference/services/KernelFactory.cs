using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using Shared;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

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
    private readonly Dictionary<string, Kernel> kernels = [];
    private int llmIndex = 0;
    private int embeddingIndex = 0;

    private Kernel CreateKernel(bool isForInference, Action<IKernelBuilder, HttpClient> func)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        var httpClient = isForInference
            ? this.httpClientFactory.CreateClient("openai-with-retry")
            : this.httpClientFactory.CreateClient("openai-without-retry");

        func(kernelBuilder, httpClient);

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

    public async Task<Kernel?> GetOrCreateLlmKernelAsync(bool isForInference, CancellationToken cancellationToken)
    {
        if (!this.config.LLM_CONNECTION_STRINGS.Any())
        {
            return null;
        }

        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            var details = this.config.LLM_CONNECTION_STRINGS[this.llmIndex];
            var key = $"llm.{this.llmIndex}";

            if (this.kernels.TryGetValue(key, out var kernel))
                return kernel;

            kernel = CreateKernel(isForInference, (builder, httpClient) =>
            {
                builder.AddAzureOpenAIChatCompletion(
                    details.DeploymentName,
                    details.Endpoint,
                    details.ApiKey,
                    httpClient: httpClient);
            });

            this.kernels.Add(key, kernel);

            llmIndex++;
            if (llmIndex >= this.config.LLM_CONNECTION_STRINGS.Count)
            {
                llmIndex = 0;
            }

            return kernel;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task<Kernel?> GetOrCreateEmbeddingKernelAsync(bool isForInference, CancellationToken cancellationToken)
    {
        if (!this.config.EMBEDDING_CONNECTION_STRINGS.Any())
        {
            return null;
        }

        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            var details = this.config.EMBEDDING_CONNECTION_STRINGS[this.llmIndex];
            var key = $"embedding.{this.embeddingIndex}";

            if (this.kernels.TryGetValue(key, out var kernel))
                return kernel;

            kernel = CreateKernel(isForInference, (builder, httpClient) =>
            {
                builder.AddAzureOpenAITextEmbeddingGeneration(
                    details.DeploymentName,
                    details.Endpoint,
                    details.ApiKey,
                    httpClient: httpClient);
            });

            this.kernels.Add(key, kernel);

            embeddingIndex++;
            if (embeddingIndex >= this.config.EMBEDDING_CONNECTION_STRINGS.Count)
            {
                embeddingIndex = 0;
            }

            return kernel;
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}