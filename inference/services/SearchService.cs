using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.SemanticKernel.Embeddings;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Inference;

public class SearchService
{
    private readonly IConfig config;
    private readonly SearchClient searchClient;
    private readonly IWorkflowContext context;
    private readonly KernelFactory kernelFactory;

    public SearchService(IConfig config, IWorkflowContext context, KernelFactory kernelFactory)
    {
        this.config = config;

        AzureKeyCredential credential = new(config.SEARCH_API_KEY);
        this.searchClient = new(new Uri(config.SEARCH_ENDPOINT_URI), config.SEARCH_INDEX, credential);

        this.context = context;
        this.kernelFactory = kernelFactory;
    }

    public async IAsyncEnumerable<Doc> SearchAsync(
        string text,
        int limit = 5,
        double minRelevanceScore = 0.7,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // create the vector query
        var kernel = this.context.IsForInference
            ? await this.kernelFactory.GetOrCreateKernelForInferenceAsync(cancellationToken)
            : await this.kernelFactory.GetOrCreateKernelForEvaluationAsync(cancellationToken);
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
        options.Select.Add("title");
        options.Select.Add("chunk_id");
        options.Select.Add("chunk");
        options.Select.Add("game_name");
        options.Select.Add("edition");

        // submit the query
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(this.config.MAX_TIMEOUT_IN_SECONDS));
        var searchResults = await searchClient.SearchAsync<Doc>(options, cancellationToken);

        // get back results async
        await foreach (SearchResult<Doc>? response in searchResults.Value.GetResultsAsync())
        {
            if (response is null || response.Score < minRelevanceScore)
            {
                continue;
            }

            yield return response.Document;
        }
    }
}