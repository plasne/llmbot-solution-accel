using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Jsonata.Net.Native;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Inference;

public class SearchService
{
    private readonly IConfig config;
    private readonly SearchClient searchClient;
    private readonly IWorkflowContext context;
    private readonly KernelFactory kernelFactory;
    private readonly IMemory memory;

    public SearchService(IConfig config, IWorkflowContext context, KernelFactory kernelFactory, IMemory memory)
    {
        this.config = config;

        AzureKeyCredential credential = new(config.SEARCH_API_KEY);
        this.searchClient = new(new Uri(config.SEARCH_ENDPOINT_URI), config.SEARCH_INDEX, credential);

        this.context = context;
        this.kernelFactory = kernelFactory;
        this.memory = memory;
    }

    private async Task<List<Doc>> SearchAsync(
        SearchOptions options,
        double minRelevanceScore = 0.7,
        CancellationToken cancellationToken = default)
    {
        var list = new List<Doc>();
        var searchResults = await searchClient.SearchAsync<Doc>(options, cancellationToken);
        await foreach (var response in searchResults.Value.GetResultsAsync())
        {
            if (response is null || response.Score < minRelevanceScore) continue;
            list.Add(response.Document);
        }
        return list;
    }

    private async Task<List<Doc>> SearchAsyncWithTransform(
        SearchOptions options,
        double minRelevanceScore,
        string template,
        CancellationToken cancellationToken = default)
    {
        var list = new List<Doc>();
        var searchResults = await searchClient.SearchAsync<SearchDocument>(options, cancellationToken);
        await foreach (var response in searchResults.Value.GetResultsAsync())
        {
            if (response is null || response.Score < minRelevanceScore) continue;
            var before = JsonConvert.SerializeObject(response);
            var query = new JsonataQuery(template);
            var after = query.Eval(before);
            var doc = JsonConvert.DeserializeObject<Doc>(after);
            if (doc is not null) list.Add(doc);
        }
        return list;
    }

    public async Task<List<Doc>> SearchAsync(
        string text,
        int limit = 5,
        double minRelevanceScore = 0.7,
        CancellationToken cancellationToken = default)
    {
        // create the vector query
        var kernel = this.context.IsForInference
            ? await this.kernelFactory.GetOrCreateKernelForInferenceAsync(context.LLMEndpointIndex, cancellationToken)
            : await this.kernelFactory.GetOrCreateKernelForEvaluationAsync(context.LLMEndpointIndex, cancellationToken);
        var embedding = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        ReadOnlyMemory<float> vector = await embedding.GenerateEmbeddingAsync(text, kernel, cancellationToken);
        VectorizedQuery vectorQuery = new(vector)
        {
            KNearestNeighborsCount = limit,
        };
        foreach (var field in this.config.SEARCH_VECTOR_FIELDS)
        {
            vectorQuery.Fields.Add(field);
        }
        var vectorSearch = new VectorSearchOptions();
        vectorSearch.Queries.Add(vectorQuery);

        // define the search options
        var options = new SearchOptions
        {
            Size = limit,
            VectorSearch = vectorSearch,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = this.config.SEARCH_SEMANTIC_CONFIG
            },
        };
        foreach (var field in this.config.SEARCH_SELECT_FIELDS)
        {
            options.Select.Add(field);
        }

        // get or set the transform template
        var template = string.IsNullOrEmpty(this.config.SEARCH_TRANSFORM_FILE)
            ? null
            : await this.memory.GetOrSet("doc:transform", null, () =>
                {
                    return File.ReadAllTextAsync(this.config.SEARCH_TRANSFORM_FILE);
                });

        // submit the query
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(this.config.MAX_TIMEOUT_IN_SECONDS));
        if (template is null)
        {
            return await SearchAsync(options, minRelevanceScore, cts.Token);
        }
        else
        {
            return await SearchAsyncWithTransform(options, minRelevanceScore, template, cts.Token);
        }
    }
}