using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

[Route("api/workflow")]
[ApiController]
public class WorkflowController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkflowResponse>> RunWorkflow(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] GroundingData groundingData)
    {
        using var scope = serviceProvider.CreateScope();
        var workflow = scope.ServiceProvider.GetRequiredService<Workflow>();
        var answer = await workflow.Execute(scope, groundingData);
        return Ok(new WorkflowResponse(answer));
    }

    [HttpPost("determine-intent")]
    public async Task<ActionResult<Intent>> DetermineIntent(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] GroundingData groundingData)
    {
        using var scope = serviceProvider.CreateScope();
        var determineIntent = scope.ServiceProvider.GetRequiredService<DetermineIntent>();
        var intent = await determineIntent.Execute(groundingData);
        return Ok(intent);
    }

    [HttpPost("get-documents")]
    public async Task<ActionResult<List<Doc>>> GetDocuments(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] Intent intent)
    {
        using var scope = serviceProvider.CreateScope();
        var getDocuments = scope.ServiceProvider.GetRequiredService<GetDocuments>();
        var docs = await getDocuments.Execute(intent);
        return Ok(docs);
    }

    [HttpPost("select-grounding-data")]
    public async Task<ActionResult<GroundingData>> SelectGroundingData(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] GroundingData input)
    {
        using var scope = serviceProvider.CreateScope();
        var selectGroundingData = scope.ServiceProvider.GetRequiredService<SelectGroundingData>();
        var output = await selectGroundingData.Execute(input);
        return Ok(output);
    }

    [HttpPost("generate-answer")]
    public async Task<ActionResult<string>> GenerateAnswer(
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] IntentAndData input)
    {
        using var scope = serviceProvider.CreateScope();
        var generateAnswer = scope.ServiceProvider.GetRequiredService<GenerateAnswer>();
        var answer = await generateAnswer.Execute(input);
        return Ok(answer);
    }
}