using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared;

namespace Inference;

[SuppressMessage(
    "Major Code Smell",
    "S107:Methods should not have too many parameters",
    Justification = "Required for dependency injection")]
public class InDomainOnlyWorkflow(
    IWorkflowContext context,
    InDomainOnlyIntent inDomainOnly,
    ApplyIntent applyIntent,
    IGetDocuments getDocuments,
    SortDocuments sortDocuments,
    SelectGroundingData selectGroundingData,
    GenerateAnswer generateAnswer,
    ILogger<InDomainOnlyWorkflow> logger)
    : IWorkflow
{
    private readonly IWorkflowContext context = context;
    private readonly InDomainOnlyIntent inDomainOnly = inDomainOnly;
    private readonly ApplyIntent applyIntent = applyIntent;
    private readonly IGetDocuments getDocuments = getDocuments;
    private readonly SortDocuments sortDocuments = sortDocuments;
    private readonly SelectGroundingData selectGroundingData = selectGroundingData;
    private readonly GenerateAnswer generateAnswer = generateAnswer;
    private readonly ILogger<InDomainOnlyWorkflow> logger = logger;

    public async Task<WorkflowResponse> Execute(
        WorkflowRequest workflowRequest,
        CancellationToken cancellationToken = default)
    {
        var response = new WorkflowResponse { Config = this.context.Config };
        try
        {
            // STEP 1: in-domain only intent
            response.Steps.Add(this.inDomainOnly.StepResponse);
            var inDomainOnlyOutput = await this.inDomainOnly.Execute(workflowRequest, cancellationToken);
            if (!this.inDomainOnly.Continue)
                return response;

            // STEP 2: apply intent
            response.Steps.Add(this.applyIntent.StepResponse);
            _ = await this.applyIntent.Execute(inDomainOnlyOutput, cancellationToken);
            if (!this.applyIntent.Continue)
                return response;

            // STEP 3: get documents
            response.Steps.Add(this.getDocuments.StepResponse);
            var getDocumentsOutput = await this.getDocuments.Execute(inDomainOnlyOutput, cancellationToken);
            if (!this.getDocuments.Continue)
                return response;

            // STEP 4: sort documents
            response.Steps.Add(this.sortDocuments.StepResponse);
            var sortDocumentsOutput = await this.sortDocuments.Execute(getDocumentsOutput, cancellationToken);
            if (!this.sortDocuments.Continue)
                return response;

            // STEP 5: select grounding data
            var selectGroundingDataInput = new GroundingData { UserQuery = inDomainOnlyOutput.Query, Docs = sortDocumentsOutput, History = workflowRequest.History };
            response.Steps.Add(this.selectGroundingData.StepResponse);
            var selectGroundingDataOutput = await this.selectGroundingData.Execute(selectGroundingDataInput, cancellationToken);
            if (!this.selectGroundingData.Continue)
                return response;

            // STEP 6: generate answer
            var generateAnswerInput = new IntentAndData { Intent = inDomainOnlyOutput, Data = selectGroundingDataOutput };
            response.Steps.Add(this.generateAnswer.StepResponse);
            var generateAnswerOutput = await this.generateAnswer.Execute(generateAnswerInput, cancellationToken);
            if (!this.generateAnswer.Continue)
                return response;

            response.Answer = generateAnswerOutput;
            return response;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred while executing the in-domain-only workflow.");
            throw new HttpWithResponseException(500, $"{ex.GetType()}: {ex.Message}", response);
        }
    }
}