using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpToken;

namespace Inference;

public class PickDocuments(IWorkflowContext context, SearchService searchService, ILogger<GetDocuments> logger)
    : BaseStep<DeterminedIntent, List<Doc>>(logger)
{
    private readonly IWorkflowContext context = context;

    public override string Name => "PickDocuments";

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
            parts.Add($"ground_truth_urls:\"{uri}\"");
        }
        var query = string.Join(" OR ", parts);

        // get the documents
        List<Doc> docs = [];
        var results = await searchService.GetDocumentsAsync(query, cancellationToken: cancellationToken);
        foreach (var result in results)
        {
            docs.Add(result);
        }
        return [.. docs];
    }
}
