using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Inference;

[Route("api/workflow")]
[ApiController]
public class WorkflowController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkflowResponse>> RunWorkflow(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] GroundingData groundingData,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var workflow = scope.ServiceProvider.GetRequiredService<Workflow>();
        var response = await workflow.Execute(groundingData, cancellationToken);
        return Ok(response);
    }

    [HttpPost("determine-intent")]
    public async Task<ActionResult<DeterminedIntent>> DetermineIntent(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] GroundingData groundingData,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var determineIntent = scope.ServiceProvider.GetRequiredService<DetermineIntent>();
        var intent = await determineIntent.Execute(groundingData, cancellationToken);
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