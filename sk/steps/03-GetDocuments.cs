using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class GetDocuments(IContext context, SearchService searchService, ILogger<GetDocuments> logger)
    : BaseStep<IDeterminedIntent, List<IDoc>>(logger)
{
    private readonly IContext context = context;

    public override string Name => "GetDocuments";

    public override async Task<List<IDoc>> ExecuteInternal(
        IDeterminedIntent intent,
        CancellationToken cancellationToken = default)
    {
        // set status
        await this.context.Stream("Getting documents...");

        // getting documents
        var docs = new List<IDoc>();
        if (intent.SearchQueries is not null)
        {
            foreach (var query in intent.SearchQueries)
            {
                await foreach (var result in searchService.SearchAsync(query, cancellationToken: cancellationToken))
                {
                    docs.Add(result);
                }
            }
        }
        return docs;
    }
}