using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpToken;

namespace Inference;

public class GetDocuments(IConfig config, IWorkflowContext context, SearchService searchService, ILogger<GetDocuments> logger)
    : BaseStep<DeterminedIntent, List<Doc>>(logger)
{
    private readonly IConfig config = config;

    private readonly IWorkflowContext context = context;
    private readonly SemaphoreSlim semaphore = new(context.Config.MAX_CONCURRENT_SEARCHES);

    public override string Name => "GetDocuments";

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
            logger.LogWarning("Exceeding the max number of queries allowed per intent by {numOfSkippedQueries}. Only the first {config.MAX_SEARCH_QUERIES_PER_INTENT} queries will be searched.", numOfSkippedQueries, this.context.Config.MAX_SEARCH_QUERIES_PER_INTENT);
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
                var results = await searchService.SearchAsync(query, cancellationToken: cancellationToken);
                if (this.config.SEARCH_MODE is SearchMode.Vector
                    or SearchMode.Hybrid
                    or SearchMode.HybridWithSemanticRerank)
                {
                    var encoding = GptEncoding.GetEncoding(context.Config.EMBEDDING_ENCODING_NAME);
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
