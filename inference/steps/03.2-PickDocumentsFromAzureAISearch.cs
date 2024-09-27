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

public class PickDocumentsFromAzureAISearch(
    IWorkflowContext context,
    IMemory memory,
    ILogger<GetDocumentsFromAzureAISearch> logger)
    : AzureAISearchBaseStep<DeterminedIntent, List<Doc>>(context, memory, logger), IGetDocuments
{
    private readonly IWorkflowContext context = context;

    public override string Name => "PickDocuments";

    private async Task<IList<Doc>> GetDocumentsAsync(string text, CancellationToken cancellationToken = default)
    {
        var options = new SearchOptions
        {
            SearchMode = Azure.Search.Documents.Models.SearchMode.All,
            QueryType = SearchQueryType.Full,
        };

        foreach (var field in this.context.Config.SEARCH_SELECT_FIELDS)
        {
            options.Select.Add(field);
        }

        // get or set the transform template
        var transformQuery = await this.GetTransformQuery(cancellationToken);

        // submit the query
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(this.context.Config.MAX_TIMEOUT_IN_SECONDS));
        return transformQuery is null
            ? await this.SearchAsync(text, options, true, cts.Token)
            : await this.SearchAsyncWithTransform(text, options, transformQuery, true, cts.Token);
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
