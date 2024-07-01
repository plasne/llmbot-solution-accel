using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Shared;

namespace Inference;

public class PrimaryWorkflow(
    DetermineIntent determineIntent,
    ApplyIntent applyIntent,
    GetDocuments getDocuments,
    SelectGroundingData selectGroundingData,
    GenerateAnswer generateAnswer,
    ILogger<PrimaryWorkflow> logger)
    : IWorkflow
{
    private readonly DetermineIntent determineIntent = determineIntent;
    private readonly ApplyIntent applyIntent = applyIntent;
    private readonly GetDocuments getDocuments = getDocuments;
    private readonly SelectGroundingData selectGroundingData = selectGroundingData;
    private readonly GenerateAnswer generateAnswer = generateAnswer;
    private readonly ILogger<PrimaryWorkflow> logger = logger;

    public async Task<WorkflowResponse> Execute(
        WorkflowRequest workflowRequest,
        CancellationToken cancellationToken = default)
    {
        var response = new WorkflowResponse();
        try
        {
            // STEP 1: determine intent
            var step1 = new WorkflowStepResponse<WorkflowRequest, DeterminedIntent>("DetermineIntent", workflowRequest, this.determineIntent.Logs, this.determineIntent.Usage);
            response.Steps.Add(step1);
            step1.Output = await this.determineIntent.Execute(workflowRequest, cancellationToken);

            // STEP 2: apply intent
            var step2 = new WorkflowStepResponse<DeterminedIntent, AppliedIntent>("ApplyIntent", step1.Output, this.applyIntent.Logs, this.applyIntent.Usage);
            response.Steps.Add(step2);
            step2.Output = await this.applyIntent.Execute(step1.Output, cancellationToken);
            if (!step2.Output.Continue)
            {
                return response;
            }

            // STEP 3: get documents
            var step3 = new WorkflowStepResponse<DeterminedIntent, List<Doc>>("GetDocuments", step1.Output, this.getDocuments.Logs, this.getDocuments.Usage);
            response.Steps.Add(step3);
            step3.Output = await this.getDocuments.Execute(step1.Output, cancellationToken);
            if (step3.Output.Count == 0)
            {
                return response;
            }

            // STEP 4: select grounding data
            var step4Input = new GroundingData { UserQuery = step1.Input.UserQuery, Docs = step3.Output, History = workflowRequest.History };
            var step4 = new WorkflowStepResponse<GroundingData, GroundingData>("SelectGroundingData", step4Input, this.selectGroundingData.Logs, this.selectGroundingData.Usage);
            response.Steps.Add(step4);
            step4.Output = await this.selectGroundingData.Execute(step4Input, cancellationToken);

            // STEP 5: generate answer
            var step5Input = new IntentAndData { Intent = step1.Output, Data = step4.Output };
            var step5 = new WorkflowStepResponse<IntentAndData, Answer>("GenerateAnswer", step5Input, this.generateAnswer.Logs, this.generateAnswer.Usage);
            response.Steps.Add(step5);
            step5.Output = await this.generateAnswer.Execute(step5Input, cancellationToken);
            if (step5.Output.Text.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return response;
            }

            response.Answer = step5.Output;
            return response;
        }
        catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            this.logger.LogError(ex, "There were too many requests against the OpenAI service...");
            throw new HttpWithResponseException(429, ex.Message, response);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred while executing the primary workflow...");
            throw new HttpWithResponseException(500, $"{ex.GetType()}: {ex.Message}", response);
        }
    }
}