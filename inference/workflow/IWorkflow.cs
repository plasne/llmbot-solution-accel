using System.Threading;
using System.Threading.Tasks;

namespace Inference;

public interface IWorkflow
{
    Task<WorkflowResponse> Execute(WorkflowRequest workflowRequest, CancellationToken cancellationToken = default);

}