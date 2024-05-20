using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inference;

[Route("api/workflow")]
[ApiController]
public class WorkflowController() : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkflowResponse>> RunWorkflow(
        [FromServices] IConfig config,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<WorkflowController> logger,
        [FromHeader(Name = "x-run-id")] string? runId,
        [FromBody] WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        // create the workflow
        using var scope = serviceProvider.CreateScope();
        var workflow = scope.ServiceProvider.GetRequiredService<Workflow>();

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

        // add some response headers
        int promptTokenCount = 0, completionTokenCount = 0;
        response.Steps.ForEach(step =>
        {
            promptTokenCount += step.Usage.PromptTokenCount;
            completionTokenCount += step.Usage.CompletionTokenCount;
        });
        Response.Headers.Append("x-metric-inf_prompt_token_count", promptTokenCount.ToString());
        Response.Headers.Append("x-metric-inf_completion_token_count", completionTokenCount.ToString());
        if (config.COST_PER_PROMPT_TOKEN > 0 && config.COST_PER_COMPLETION_TOKEN > 0)
        {
            var cost = promptTokenCount * config.COST_PER_PROMPT_TOKEN + completionTokenCount * config.COST_PER_COMPLETION_TOKEN;
            Response.Headers.Append("x-metric-inf_cost", cost.ToString());
        }

        return Ok(response);
    }

    [HttpPost("determine-intent")]
    public async Task<ActionResult<DeterminedIntent>> DetermineIntent(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var determineIntent = scope.ServiceProvider.GetRequiredService<DetermineIntent>();
        var intent = await determineIntent.Execute(request, cancellationToken);
        return Ok(intent);
    }

    [HttpPost("get-documents")]
    public async Task<ActionResult<List<Doc>>> GetDocuments(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] DeterminedIntent intent,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var getDocuments = scope.ServiceProvider.GetRequiredService<GetDocuments>();
        var docs = await getDocuments.Execute(intent, cancellationToken);
        return Ok(docs);
    }

    [HttpPost("select-grounding-data")]
    public async Task<ActionResult<GroundingData>> SelectGroundingData(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] GroundingData input,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var selectGroundingData = scope.ServiceProvider.GetRequiredService<SelectGroundingData>();
        var output = await selectGroundingData.Execute(input, cancellationToken);
        return Ok(output);
    }

    [HttpPost("generate-answer")]
    public async Task<ActionResult<string>> GenerateAnswer(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] IntentAndData input,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var generateAnswer = scope.ServiceProvider.GetRequiredService<GenerateAnswer>();
        var answer = await generateAnswer.Execute(input, cancellationToken);
        return Ok(answer);
    }
}