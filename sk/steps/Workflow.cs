using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared;

public class Workflow(
    DetermineIntent determineIntent,
    ApplyIntent applyIntent,
    GetDocuments getDocuments,
    SelectGroundingData selectGroundingData,
    GenerateAnswer generateAnswer)
{
    private readonly DetermineIntent determineIntent = determineIntent;
    private readonly ApplyIntent applyIntent = applyIntent;
    private readonly GetDocuments getDocuments = getDocuments;
    private readonly SelectGroundingData selectGroundingData = selectGroundingData;
    private readonly GenerateAnswer generateAnswer = generateAnswer;

    public async Task<IWorkflowResponse> Execute(
        IGroundingData groundingData,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticService.Source.StartActivity("Workflow");
        var response = new WorkflowResponse();
        try
        {
            // STEP 1: determine intent
            var step1 = new WorkflowStepResponse<IGroundingData, IDeterminedIntent>("DetermineIntent", groundingData, this.determineIntent.Logs);
            response.Steps.Add(step1);
            step1.Output = await this.determineIntent.Execute(groundingData, cancellationToken);

            // STEP 2: apply intent
            var step2 = new WorkflowStepResponse<IDeterminedIntent, IAppliedIntent>("ApplyIntent", step1.Output, this.applyIntent.Logs);
            response.Steps.Add(step2);
            step2.Output = await this.applyIntent.Execute(step1.Output, cancellationToken);
            if (step2.Output.Continue == false)
            {
                return response;
            }

            // STEP 2: get documents
            var step3 = new WorkflowStepResponse<IDeterminedIntent, List<IDoc>>("GetDocuments", step1.Output, this.getDocuments.Logs);
            response.Steps.Add(step3);
            step3.Output = await this.getDocuments.Execute(step1.Output, cancellationToken);

            // STEP 3: select grounding data
            var step4Input = new GroundingData(step1.Input.UserQuery) { Docs = step3.Output, History = groundingData.History };
            var step4 = new WorkflowStepResponse<IGroundingData, IGroundingData>("SelectGroundingData", step4Input, this.selectGroundingData.Logs);
            response.Steps.Add(step4);
            step4.Output = await this.selectGroundingData.Execute(step4Input, cancellationToken);

            // STEP 4: generate answer
            var step5Input = new IntentAndData { Intent = step1.Output, Data = step4.Output };
            var step5 = new WorkflowStepResponse<IIntentAndData, IAnswer>("GenerateAnswer", step5Input, this.generateAnswer.Logs);
            response.Steps.Add(step5);
            step5.Output = await this.generateAnswer.Execute(step5Input, cancellationToken);

            response.Answer = step5.Output;
            return response;
        }
        catch (Exception ex)
        {
            throw new HttpExceptionWithResponse(500, ex.Message, response);
        }
    }
}