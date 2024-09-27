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
using SharpToken;

namespace Inference;

public class PickDocumentsFromBicycleShop(
    IWorkflowContext context,
    ILogger<GetDocumentsFromAzureAISearch> logger)
    : BicycleShopSearchBaseStep<DeterminedIntent, List<Doc>>(logger), IGetDocuments
{
    private readonly IWorkflowContext context = context;

    public override string Name => "PickDocuments";

    public Task<IList<Doc>> GetDocumentsAsync(string text, CancellationToken cancellationToken = default)
    {
        List<Doc> docs = [];
        foreach (var part in text.Split(" OR "))
        {
            var kv = part.Split(":", 2);
            if (kv.Length == 2)
            {
                var uri = kv[1].Trim('"');
                if (this.BicyleDocs.ContainsKey(uri))
                {
                    docs.Add(new Doc
                    {
                        Title = uri,
                        Urls = [uri],
                        Content = this.BicyleDocs[uri],
                    });
                }
            }
        }
        return Task.FromResult<IList<Doc>>(docs);
    }

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

        // FIX THIS!!!!!

        // require at least one uri
        if (!uris.Any())
        {
            return new List<Doc>();
        }

        // build the query
        List<string> parts = [];
        foreach (var uri in uris)
        {
            parts.Add($"{this.context.Config.PICK_DOCS_URL_FIELD}:\"{uri}\"");
        }
        var query = string.Join(" OR ", parts);

        // get the documents
        List<Doc> docs = [];
        var results = await this.GetDocumentsAsync(query, cancellationToken: cancellationToken);
        foreach (var result in results)
        {
            docs.Add(result);
        }
        return [.. docs];
    }
}
