using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Jsonata.Net.Native;
using Microsoft.SemanticKernel.Embeddings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzureSearchMode = Azure.Search.Documents.Models.SearchMode;

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
        string text,
        SearchOptions options,
        bool includeText,
        CancellationToken cancellationToken = default)
    {
        var searchResults = includeText
            ? await searchClient.SearchAsync<Doc>(text, options, cancellationToken)
            : await searchClient.SearchAsync<Doc>(options, cancellationToken);
        var list = new List<Doc>();
        await foreach (var response in searchResults.Value.GetResultsAsync())
        {
            if (response is null)
                continue;

            // check reranker scores
            if (this.config.SEARCH_MODE is SearchMode.KeywordWithSemanticRerank
                or SearchMode.HybridWithSemanticRerank)
            {
                if (response.SemanticSearch.RerankerScore < (double)this.config.MIN_RELEVANCE_RERANK_SCORE)
                    continue;
            }

            if (response.Score < (double)this.config.MIN_RELEVANCE_SEARCH_SCORE)
                continue;
            list.Add(response.Document);
        }
        return list;
    }

    private async Task<List<Doc>> SearchAsyncWithTransform(
        string text,
        SearchOptions options,
        JsonataQuery query,
        bool includeText,
        CancellationToken cancellationToken = default)
    {
        var searchResults = includeText
            ? await searchClient.SearchAsync<SearchDocument>(text, options, cancellationToken)
            : await searchClient.SearchAsync<SearchDocument>(options, cancellationToken);

        var list = new List<Doc>();
        await foreach (var response in searchResults.Value.GetResultsAsync())
        {
            if (response is null)
                continue;

            // check reranker scores
            if (this.config.SEARCH_MODE is SearchMode.KeywordWithSemanticRerank
                or SearchMode.HybridWithSemanticRerank)
            {
                if (response.SemanticSearch.RerankerScore < (double)this.config.MIN_RELEVANCE_RERANK_SCORE)
                    continue;
            }

            if (response.Score < (double)this.config.MIN_RELEVANCE_SEARCH_SCORE)
                continue;
            var before = JsonConvert.SerializeObject(response);
            var after = query.Eval(before);
            var doc = JsonConvert.DeserializeObject<Doc>(after);
            if (doc is not null)
                list.Add(doc);
        }
        return list;
    }

    private async Task<JsonataQuery?> GetTransformQuery(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(this.config.SEARCH_TRANSFORM_FILE))
            return null;
        var template = await this.memory.GetOrSet("doc:transform", null, () =>
        {
            return File.ReadAllTextAsync(this.config.SEARCH_TRANSFORM_FILE, cancellationToken);
        });
        return new JsonataQuery(template);
    }

    public async Task<List<Doc>> GetDocumentsAsync(
       string text,
       CancellationToken cancellationToken = default)
    {
        var options = new SearchOptions
        {
            SearchMode = AzureSearchMode.All,
            QueryType = SearchQueryType.Full,
        };

        foreach (var field in this.config.SEARCH_SELECT_FIELDS)
        {
            options.Select.Add(field);
        }

        // get or set the transform template
        var transformQuery = await this.GetTransformQuery(cancellationToken);

        // submit the query
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(this.config.MAX_TIMEOUT_IN_SECONDS));
        return transformQuery is null
            ? await this.SearchAsync(text, options, true, cts.Token)
            : await this.SearchAsyncWithTransform(text, options, transformQuery, true, cts.Token);
    }

    public async Task<List<Doc>> SearchAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        // define the search options
        var options = new SearchOptions()
        {
            Size = this.config.SEARCH_TOP,
        };

        foreach (var field in this.config.SEARCH_SELECT_FIELDS)
        {
            options.Select.Add(field);
        }

        if (this.config.SEARCH_MODE is SearchMode.Vector
            or SearchMode.Hybrid
            or SearchMode.HybridWithSemanticRerank)
        {
            // create the vector query
            var kernel = this.context.IsForInference
                ? await this.kernelFactory.GetOrCreateKernelForInferenceAsync(context.LLMEndpointIndex, cancellationToken)
                : await this.kernelFactory.GetOrCreateKernelForEvaluationAsync(context.LLMEndpointIndex, cancellationToken);
            var embedding = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            ReadOnlyMemory<float> vector = await embedding.GenerateEmbeddingAsync(text, kernel, cancellationToken);
            VectorizedQuery vectorQuery = new(vector)
            {
                Exhaustive = this.config.SEARCH_VECTOR_EXHAUST_KNN,
                KNearestNeighborsCount = this.config.SEARCH_KNN,
            };

            foreach (var field in this.config.SEARCH_VECTOR_FIELDS)
            {
                vectorQuery.Fields.Add(field);
            }
            var vectorSearch = new VectorSearchOptions
            {
                FilterMode = VectorFilterMode.PreFilter // the sdk default
            };
            vectorSearch.Queries.Add(vectorQuery);
            options.VectorSearch = vectorSearch;
        }

        if (this.config.SEARCH_MODE is SearchMode.KeywordWithSemanticRerank
            or SearchMode.HybridWithSemanticRerank)
        {
            // enable semantic search reranking
            options.QueryType = SearchQueryType.Semantic;
            options.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = this.config.SEARCH_SEMANTIC_RERANK_CONFIG
            };
        }

        // get or set the transform template
        var transformQuery = await this.GetTransformQuery(cancellationToken);

        // submit the query
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(this.config.MAX_TIMEOUT_IN_SECONDS));
        var includeText = this.config.SEARCH_MODE != SearchMode.Vector;
        return transformQuery is null
            ? await this.SearchAsync(text, options, includeText, cts.Token)
            : await this.SearchAsyncWithTransform(text, options, transformQuery, includeText, cts.Token);
    }
}