using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Jsonata.Net.Native;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Inference;

public abstract class AzureAISearchBaseStep<TInput, TOutput> : BaseStep<TInput, TOutput>
{
    public AzureAISearchBaseStep(IWorkflowContext context, IMemory memory, ILogger logger)
        : base(logger)
    {
        this.context = context;
        this.memory = memory;

        AzureKeyCredential credential = new(this.context.Config.SEARCH_API_KEY);
        this.searchClient = new(new Uri(this.context.Config.SEARCH_ENDPOINT_URI), this.context.Config.SEARCH_INDEX, credential);
    }

    private readonly IWorkflowContext context;
    private readonly IMemory memory;
    private readonly SearchClient searchClient;

    protected async Task<List<Doc>> SearchAsync(
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
            if (this.context.Config.SEARCH_MODE is SearchMode.KeywordWithSemanticRerank
                or SearchMode.HybridWithSemanticRerank)
            {
                if (response.SemanticSearch.RerankerScore < (double)this.context.Config.MIN_RELEVANCE_RERANK_SCORE)
                    continue;
            }

            if (response.Score < (double)this.context.Config.MIN_RELEVANCE_SEARCH_SCORE)
                continue;
            list.Add(response.Document);
        }
        return list;
    }

    protected async Task<List<Doc>> SearchAsyncWithTransform(
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
            if (this.context.Config.SEARCH_MODE is SearchMode.KeywordWithSemanticRerank
                or SearchMode.HybridWithSemanticRerank)
            {
                if (response.SemanticSearch.RerankerScore < (double)this.context.Config.MIN_RELEVANCE_RERANK_SCORE)
                    continue;
            }

            if (response.Score < (double)this.context.Config.MIN_RELEVANCE_SEARCH_SCORE)
                continue;
            var before = JsonConvert.SerializeObject(response);
            var after = query.Eval(before);
            var doc = JsonConvert.DeserializeObject<Doc>(after);
            if (doc is not null)
                list.Add(doc);
        }
        return list;
    }

    protected async Task<JsonataQuery?> GetTransformQuery(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(this.context.Config.SEARCH_TRANSFORM_FILE))
            return null;
        var template = await this.memory.GetOrSet("doc:transform", null, () =>
        {
            return File.ReadAllTextAsync(this.context.Config.SEARCH_TRANSFORM_FILE, cancellationToken);
        });
        return new JsonataQuery(template);
    }

}