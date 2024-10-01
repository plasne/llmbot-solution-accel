using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inference;

public interface IDetermineIntent : IStep<WorkflowRequest, DeterminedIntent>
{
}