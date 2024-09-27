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

public class GetDocumentsHardcoded(
    IWorkflowContext context,
    KernelFactory kernelFactory,
    ILogger<GetDocumentsFromAzureAISearch> logger)
    : BicycleShopSearchBaseStep<DeterminedIntent, List<Doc>>(logger), IGetDocuments
{
    private readonly IWorkflowContext context = context;
    private readonly KernelFactory kernelFactory = kernelFactory;

    public override string Name => "GetDocuments";

    private Task<IList<Doc>> SearchAsync(string text, CancellationToken cancellationToken = default)
    {
        List<Doc> docs = [];
        var keywords = text.Split(" ").Select(x => x.ToLower());
        foreach (var doc in this.BicyleDocs)
        {
            var srcwords = doc.Value.Split(" ").Select(x => x.ToLower());
            if (keywords.Any(keyword => srcwords.Contains(keyword)))
            {
                docs.Add(new Doc
                {
                    Title = doc.Key,
                    Urls = [doc.Key],
                    Content = doc.Value,
                });
            }
        }
        return Task.FromResult<IList<Doc>>(docs);
    }

    public override async Task<List<Doc>> ExecuteInternal(
        DeterminedIntent intent,
        CancellationToken cancellationToken = default)
    {
        // set status
        await this.context.Stream("Getting documents...");

        // determine the queries
        var queries = intent.SearchQueries is not null
            ? intent.SearchQueries
            : new List<string> { intent.Query };

        // use Select to create a collection of search tasks
        ConcurrentBag<Doc> docs = [];
        var tasks = queries.Take(context.Config.MAX_SEARCH_QUERIES_PER_INTENT).Select(async query =>
        {
            var results = await this.SearchAsync(query, cancellationToken: cancellationToken);
            foreach (var result in results)
            {
                docs.Add(result);
            }
        });

        // schedules all the tasks concurrently and awaits them
        await Task.WhenAll(tasks);

        // decide whether to continue or not
        this.Continue = (!this.context.Config.EXIT_WHEN_NO_DOCUMENTS || !docs.IsEmpty);

        // output
        await this.context.Stream(embeddingTokens: this.Usage.EmbeddingTokenCount);
        return [.. docs];
    }
}
