using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inference;

public interface IStep<TInput, TOutput>
{
    public string Name { get; }
    public List<LogEntry> Logs { get; }
    public Usage Usage { get; }
    public WorkflowStepResponse<TInput, TOutput> StepResponse { get; }
    public bool Continue { get; set; }

    public Task<TOutput> Execute(TInput input, CancellationToken cancellationToken = default);
}