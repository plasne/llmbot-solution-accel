using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Inference;

public class GetDocuments(IWorkflowContext context, SearchService searchService, ILogger<GetDocuments> logger)
    : BaseStep<DeterminedIntent, List<Doc>>(logger)
{
    private readonly IWorkflowContext context = context;

    public override string Name => "GetDocuments";

    public override async Task<List<Doc>> ExecuteInternal(
        DeterminedIntent intent,
        CancellationToken cancellationToken = default)
    {
        // set status
        await this.context.Stream("Getting documents...");

        // getting documents
        var docs = new List<Doc>();
        if (intent.SearchQueries is not null)
        {
            foreach (var query in intent.SearchQueries)
            {
                var results = await searchService.SearchAsync(query, cancellationToken: cancellationToken);
                foreach (var result in results)
                {
                    docs.Add(result);
                }
            }
        }
        return docs;
    }
}