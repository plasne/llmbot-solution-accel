using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class GetDocuments(IContext context, SearchService searchService, ILogger<GetDocuments> logger)
    : BaseStep<Intent, List<Doc>>(logger)
{
    private readonly IContext context = context;

    public override string Name => "GetDocuments";

    public override async Task<List<Doc>> ExecuteInternal(Intent intent)
    {
        // set status
        await this.context.SetStatus("Getting documents...");

        // getting documents
        var docs = new List<Doc>();
        if (intent.SearchQueries is not null)
        {
            foreach (var query in intent.SearchQueries)
            {
                await foreach (var result in searchService.SearchAsync(query))
                {
                    docs.Add(result);
                }
            }
        }
        return docs;
    }
}