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

        // if requested, add some response headers
        if (context.Config.EMIT_USAGE_AS_RESPONSE_HEADERS)
        {
            int promptTokenCount = 0, completionTokenCount = 0, embeddingTokenCount = 0;
            response.Steps.ForEach(step =>
            {
                promptTokenCount += step.Usage.PromptTokenCount;
                completionTokenCount += step.Usage.CompletionTokenCount;
                embeddingTokenCount += step.Usage.EmbeddingTokenCount;
            });
            this.Response.Headers.Append("x-metric-inf_prompt_token_count", promptTokenCount.ToString());
            this.Response.Headers.Append("x-metric-inf_completion_token_count", completionTokenCount.ToString());
            if (context.Config.COST_PER_PROMPT_TOKEN > 0 && context.Config.COST_PER_COMPLETION_TOKEN > 0 && context.Config.COST_PER_EMBEDDING_TOKEN > 0)
            {
                var cost = (promptTokenCount * context.Config.COST_PER_PROMPT_TOKEN) + (completionTokenCount * context.Config.COST_PER_COMPLETION_TOKEN) +
                  (embeddingTokenCount * context.Config.COST_PER_EMBEDDING_TOKEN);
                this.Response.Headers.Append("x-metric-inf_cost", cost.ToString());
            }
        }

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