using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using SharpToken;

namespace Inference;

public class PickDocumentsFromBicycleShop(
    IWorkflowContext context,
    ILogger<GetDocumentsFromAzureAISearch> logger)
    : BicycleShopSearchBaseStep<DeterminedIntent, List<Doc>>(logger), IPickDocuments
{
    private readonly IWorkflowContext context = context;

    public override string Name => "PickDocumentsFromBicycleShop";

    public override async Task<List<Doc>> ExecuteInternal(
        DeterminedIntent intent,
        CancellationToken cancellationToken = default)
    {
        // set status
        await this.context.Stream("Picking documents...");

        // extract uris from context
        IEnumerable<string> uris = context.WorkflowRequest?.Context?.Where(x =>
            string.IsNullOrEmpty(x) == false &&
            (
                x.StartsWith("https://", System.StringComparison.InvariantCultureIgnoreCase) ||
                x.StartsWith("http://", System.StringComparison.InvariantCultureIgnoreCase))
            )
            ?? [];

        // find documents
        List<Doc> docs = [];
        foreach (var uri in uris)
        {
            if (this.BicyleDocs.ContainsKey(uri.ToLower()))
            {
                docs.Add(new Doc
                {
                    Title = uri,
                    Urls = [uri],
                    Content = this.BicyleDocs[uri],
                });
            }
        }

        this.Continue = (!this.context.Config.EXIT_WHEN_NO_DOCUMENTS || docs.Any());
        return docs;
    }
}
