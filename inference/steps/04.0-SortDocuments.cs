using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Inference;

public class SortDocuments(ILogger<SortDocuments> logger)
    : BaseStep<List<Doc>, List<Doc>>(logger)
{
    public override string Name => "SortDocuments";

    public override Task<List<Doc>> ExecuteInternal(
        List<Doc> input,
        CancellationToken cancellationToken = default)
    {
        // sort documents by both search score and reranker score
        return Task.FromResult(input
            .OrderByDescending(doc => doc.RerankSearchScore)
            .ThenByDescending(doc => doc.SearchScore)
            .ToList());
    }
}