using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IStep<TInput, TOutput>
{
    public string Name { get; }
    public List<LogEntry> Logs { get; }

    public Task<TOutput> Execute(TInput input, CancellationToken cancellationToken = default);
}