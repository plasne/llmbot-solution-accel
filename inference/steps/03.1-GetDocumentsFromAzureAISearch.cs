using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using SharpToken;

namespace Inference;

public class GetDocumentsFromAzureAISearch(
    IWorkflowContext context,
    KernelFactory kernelFactory,
    IMemory memory,
    ILogger<GetDocumentsFromAzureAISearch> logger)
    : AzureAISearchBaseStep<DeterminedIntent, List<Doc>>(context, memory, logger), IGetDocuments
{
    private readonly IWorkflowContext context = context;
    private readonly KernelFactory kernelFactory = kernelFactory;
    private readonly SemaphoreSlim semaphore = new(context.Config.MAX_CONCURRENT_SEARCHES);

    public override string Name => "GetDocuments";

    private async Task<IList<Doc>> SearchAsync(string text, CancellationToken cancellationToken = default)
    {
        // define the search options
        var options = new SearchOptions()
        {
            Size = this.context.Config.SEARCH_TOP,
        };

        foreach (var field in this.context.Config.SEARCH_SELECT_FIELDS)
        {
            options.Select.Add(field);
        }

        if (this.context.Config.SEARCH_MODE is SearchMode.Vector
            or SearchMode.Hybrid
            or SearchMode.HybridWithSemanticRerank)
        {
            // create the vector query
            var kernel = this.context.IsForInference
                ? await this.kernelFactory.GetOrCreateKernelForInferenceAsync(context.KernelIndex, cancellationToken)
                : await this.kernelFactory.GetOrCreateKernelForEvaluationAsync(context.KernelIndex, cancellationToken);
            var embedding = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            ReadOnlyMemory<float> vector = await embedding.GenerateEmbeddingAsync(text, kernel, cancellationToken);
            VectorizedQuery vectorQuery = new(vector)
            {
                Exhaustive = this.context.Config.SEARCH_VECTOR_EXHAUST_KNN,
                KNearestNeighborsCount = this.context.Config.SEARCH_KNN,
            };

            foreach (var field in this.context.Config.SEARCH_VECTOR_FIELDS)
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

        if (this.context.Config.SEARCH_MODE is SearchMode.KeywordWithSemanticRerank
            or SearchMode.HybridWithSemanticRerank)
        {
            // enable semantic search reranking
            options.QueryType = SearchQueryType.Semantic;
            options.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = this.context.Config.SEARCH_SEMANTIC_RERANK_CONFIG
            };
        }

        // get or set the transform template
        var transformQuery = await this.GetTransformQuery(cancellationToken);

        // submit the query
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(this.context.Config.MAX_TIMEOUT_IN_SECONDS));
        var includeText = this.context.Config.SEARCH_MODE != SearchMode.Vector;
        return transformQuery is null
            ? await this.SearchAsync(text, options, includeText, cts.Token)
            : await this.SearchAsyncWithTransform(text, options, transformQuery, includeText, cts.Token);
    }

    public override async Task<List<Doc>> ExecuteInternal(
        DeterminedIntent intent,
        CancellationToken cancellationToken = default)
    {
        // set status
        await this.context.Stream("Getting documents...");

        this.LogDebug($"using MIN_RELEVANCE_SEARCH_SCORE: {context.Config.MIN_RELEVANCE_SEARCH_SCORE:0.000}...");

        // determine the queries
        var queries = intent.SearchQueries is not null
            ? intent.SearchQueries
            : new List<string> { intent.Query };

        // log if intent queries exceed the limit
        if (queries.Count > this.context.Config.MAX_SEARCH_QUERIES_PER_INTENT)
        {
            var numOfSkippedQueries = queries.Count - this.context.Config.MAX_SEARCH_QUERIES_PER_INTENT;
            this.LogWarning($"Exceeding the max number of queries allowed per intent by {numOfSkippedQueries}. Only the first {this.context.Config.MAX_SEARCH_QUERIES_PER_INTENT} queries will be searched.");
        }

        int totalEmbeddingTokenCount = 0;
        // getting documents
        ConcurrentBag<Doc> docs = [];
        // creates a linked token source to cancel all tasks if any task throws an Azure.RequestFailedException
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // use Select to create a collection of search tasks
        var tasks = queries.Take(context.Config.MAX_SEARCH_QUERIES_PER_INTENT).Select(async query =>
        {
            await semaphore.WaitAsync(cancellationToken); // Throttle the number of concurrent search tasks
            try
            {
                var results = await this.SearchAsync(query, cancellationToken: cancellationToken);
                if (this.context.Config.SEARCH_MODE is SearchMode.Vector
                    or SearchMode.Hybrid
                    or SearchMode.HybridWithSemanticRerank)
                {
                    var encoding = GptEncoding.GetEncoding(context.Config.EMBEDDING_ENCODING_MODEL);
                    var count = encoding.CountTokens(query);
                    Interlocked.Add(ref totalEmbeddingTokenCount, count);
                    DiagnosticService.RecordEmbeddingTokenCount(count, context.Config.EMBEDDING_MODEL_NAME);
                }
                foreach (var result in results)
                {
                    docs.Add(result);
                }
            }
            catch (Azure.RequestFailedException)
            {
                // fail fast on bad requests
                await cts.CancelAsync();
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        });

        // schedules all the tasks concurrently and awaits them
        await Task.WhenAll(tasks);

        // decide whether to continue or not
        this.Continue = (!this.context.Config.EXIT_WHEN_NO_DOCUMENTS || !docs.IsEmpty);

        // output
        this.Usage.EmbeddingTokenCount = totalEmbeddingTokenCount;
        await this.context.Stream(embeddingTokens: this.Usage.EmbeddingTokenCount);
        return [.. docs];
    }
}
