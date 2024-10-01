using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inference;

[Route("api/workflows")]
[ApiController]
public class WorkflowsController() : ControllerBase
{
    private async Task<ActionResult<WorkflowResponse>> RunWorkflow(
        IWorkflowContext context,
        IWorkflow workflow,
        ILogger<WorkflowsController> logger,
        string? runId,
        WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        // create the OpenTelemetry activity
        using var activity = DiagnosticService.Source.StartActivity("Workflow");
        if (!string.IsNullOrEmpty(runId))
        {
            logger.LogDebug("the workflow request will be logged as run_id {id}.", runId);
            activity?.SetBaggage("run_id", runId);
            activity?.SetTag("run_id", runId);
        }

        // execute the workflow
        var response = await workflow.Execute(request, cancellationToken);
        return this.Ok(response);
    }

    [HttpPost("primary")]
    public async Task<ActionResult<WorkflowResponse>> RunPrimaryWorkflow(
        [FromServices] IConfig config,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<WorkflowsController> logger,
        [FromHeader(Name = "x-run-id")] string? runId,
        [FromBody] WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IWorkflowContext>();
        context.IsForEvaluation = true;
        context.Config = new WorkflowConfig(config, this.Request.Headers.ToParameters());
        context.WorkflowRequest = request;
        var workflow = scope.ServiceProvider.GetRequiredService<PrimaryWorkflow>();
        return await this.RunWorkflow(context, workflow, logger, runId, request, cancellationToken);
    }

    [HttpPost("in-domain-only")]
    public async Task<ActionResult<WorkflowResponse>> RunInDomainOnlyWorkflow(
        [FromServices] IConfig config,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<WorkflowsController> logger,
        [FromHeader(Name = "x-run-id")] string? runId,
        [FromBody] WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IWorkflowContext>();
        context.IsForEvaluation = true;
        context.Config = new WorkflowConfig(config, this.Request.Headers.ToParameters());
        context.WorkflowRequest = request;
        var workflow = scope.ServiceProvider.GetRequiredService<InDomainOnlyWorkflow>();
        return await this.RunWorkflow(context, workflow, logger, runId, request, cancellationToken);
    }

    [HttpPost("pick-docs")]
    public async Task<ActionResult<WorkflowResponse>> RunPickDocumentsWorkflow(
    [FromServices] IConfig config,
    [FromServices] IServiceProvider serviceProvider,
    [FromServices] ILogger<WorkflowsController> logger,
    [FromHeader(Name = "x-run-id")] string? runId,
    [FromBody] WorkflowRequest request,
    CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IWorkflowContext>();
        context.IsForEvaluation = true;
        context.Config = new WorkflowConfig(config, this.Request.Headers.ToParameters());
        context.WorkflowRequest = request;
        var workflow = scope.ServiceProvider.GetRequiredService<PickDocumentsWorkflow>();
        return await this.RunWorkflow(context, workflow, logger, runId, request, cancellationToken);
    }
}