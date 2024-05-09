using System.Collections.Generic;

public class WorkflowResponse : IWorkflowResponse
{
    public IAnswer? Answer { get; set; }
    public IList<IWorkflowStepResponse> Steps { get; set; } = [];
}