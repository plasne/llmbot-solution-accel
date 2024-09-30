using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Shared;

namespace Inference;

[SuppressMessage(
    "Major Code Smell",
    "S107:Methods should not have too many parameters",
    Justification = "Required for dependency injection")]
public class PrimaryWorkflow(
    IWorkflowContext context,
    IDetermineIntent determineIntent,
    ApplyIntent applyIntent,
    IGetDocuments getDocuments,
    SortDocuments sortDocuments,
    SelectGroundingData selectGroundingData,
    IGenerateAnswer generateAnswer,
    ILogger<PrimaryWorkflow> logger)
    : IWorkflow
{
    private readonly IWorkflowContext context = context;
    private readonly IDetermineIntent determineIntent = determineIntent;
    private readonly ApplyIntent applyIntent = applyIntent;
    private readonly IGetDocuments getDocuments = getDocuments;
    private readonly SortDocuments sortDocuments = sortDocuments;
    private readonly SelectGroundingData selectGroundingData = selectGroundingData;
    private readonly IGenerateAnswer generateAnswer = generateAnswer;
    private readonly ILogger<PrimaryWorkflow> logger = logger;

    public async Task<WorkflowResponse> Execute(
        WorkflowRequest workflowRequest,
        CancellationToken cancellationToken = default)
    {
        var response = new WorkflowResponse { Config = this.context.Config };
        try
        {
            // STEP 1: determine intent
            response.Steps.Add(this.determineIntent.StepResponse);
            var determineIntentOutput = await this.determineIntent.Execute(workflowRequest, cancellationToken);
            if (!this.determineIntent.Continue)
                return response;

            // STEP 2: apply intent
            response.Steps.Add(this.applyIntent.StepResponse);
            _ = await this.applyIntent.Execute(determineIntentOutput, cancellationToken);
            if (!this.applyIntent.Continue)
                return response;

            // STEP 3: get documents
            response.Steps.Add(this.getDocuments.StepResponse);
            var getDocumentsOutput = await this.getDocuments.Execute(determineIntentOutput, cancellationToken);
            if (!this.getDocuments.Continue)
                return response;

            // STEP 4: sort documents
            response.Steps.Add(this.sortDocuments.StepResponse);
            var sortDocumentsOutput = await this.sortDocuments.Execute(getDocumentsOutput, cancellationToken);
            if (!this.sortDocuments.Continue)
                return response;

            // STEP 5: select grounding data
            var selectGroundingDataInput = new GroundingData { UserQuery = determineIntentOutput.Query, Docs = sortDocumentsOutput, History = workflowRequest.History };
            response.Steps.Add(this.selectGroundingData.StepResponse);
            var selectGroundingDataOutput = await this.selectGroundingData.Execute(selectGroundingDataInput, cancellationToken);
            if (!this.selectGroundingData.Continue)
                return response;

            // STEP 6: generate answer
            var generateAnswerInput = new IntentAndData { Intent = determineIntentOutput, Data = selectGroundingDataOutput };
            response.Steps.Add(this.generateAnswer.StepResponse);
            var generateAnswerOutput = await this.generateAnswer.Execute(generateAnswerInput, cancellationToken);
            if (!this.generateAnswer.Continue)
                return response;

            response.Answer = generateAnswerOutput;
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