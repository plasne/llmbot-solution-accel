using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inference;

public interface ISearchService
{
    public Task<IList<Doc>> GetDocumentsAsync(string text, CancellationToken cancellationToken = default);

    public Task<IList<Doc>> SearchAsync(string text, CancellationToken cancellationToken = default);
}