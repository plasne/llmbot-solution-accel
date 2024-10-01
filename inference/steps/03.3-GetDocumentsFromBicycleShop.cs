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
using YamlDotNet.Core.Events;

namespace Inference;

public class GetDocumentsFromBicyleShop(
    IWorkflowContext context,
    KernelFactory kernelFactory,
    ILogger<GetDocumentsFromAzureAISearch> logger)
    : BicycleShopSearchBaseStep<DeterminedIntent, List<Doc>>(logger), IGetDocuments
{
    private readonly IWorkflowContext context = context;
    private readonly KernelFactory kernelFactory = kernelFactory;

    public override string Name => "GetDocumentsFromBicyleShop";

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

        // find in the bicycle docs
        List<Doc> docs = [];
        foreach (var query in queries)
        {
            var wordsOnlyQuery = new string(query.Where(c => !char.IsPunctuation(c)).ToArray());
            var keywords = wordsOnlyQuery.Split(" ").Select(x => x.ToLower());
            foreach (var doc in this.BicyleDocs)
            {
                var worksOnlySource = new string(doc.Value.Where(c => !char.IsPunctuation(c)).ToArray());
                var srcwords = worksOnlySource.Split(" ").Select(x => x.ToLower());
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
        }

        // decide whether to continue or not
        this.Continue = (!this.context.Config.EXIT_WHEN_NO_DOCUMENTS || docs.Any());

        // output
        await this.context.Stream(embeddingTokens: this.Usage.EmbeddingTokenCount);
        return [.. docs];
    }
}
