using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Workflow(
    DetermineIntent determineIntent,
    GetDocuments getDocuments,
    SelectGroundingData selectGroundingData,
    GenerateAnswer generateAnswer)
{
    private readonly DetermineIntent determineIntent = determineIntent;
    private readonly GetDocuments getDocuments = getDocuments;
    private readonly SelectGroundingData selectGroundingData = selectGroundingData;
    private readonly GenerateAnswer generateAnswer = generateAnswer;

    public async Task<WorkflowResponse> Execute(
        GroundingData groundingData,
        CancellationToken cancellationToken = default)
    {
        var response = new WorkflowResponse();
        try
        {
            // STEP 1: determine intent
            var step1 = new WorkflowStepResponse<GroundingData, Intent>("DetermineIntent", groundingData, this.determineIntent.Logs);
            response.Steps.Add(step1);
            step1.Output = await this.determineIntent.Execute(groundingData, cancellationToken);

            // STEP 2: get documents
            var step2 = new WorkflowStepResponse<Intent, List<Doc>>("GetDocuments", step1.Output, this.getDocuments.Logs);
            response.Steps.Add(step2);
            step2.Output = await this.getDocuments.Execute(step1.Output, cancellationToken);

            // STEP 3: select grounding data
            var step3Input = new GroundingData { Docs = step2.Output, History = groundingData.History };
            var step3 = new WorkflowStepResponse<GroundingData, GroundingData>("SelectGroundingData", step3Input, this.selectGroundingData.Logs);
            response.Steps.Add(step3);
            step3.Output = await this.selectGroundingData.Execute(step3Input, cancellationToken);

            // STEP 4: generate answer
            var step4Input = new IntentAndData { Intent = step1.Output, Data = step3.Output };
            var step4 = new WorkflowStepResponse<IntentAndData, Answer>("GenerateAnswer", step4Input, this.generateAnswer.Logs);
            response.Steps.Add(step4);
            step4.Output = await this.generateAnswer.Execute(step4Input, cancellationToken);

            response.Answer = step4.Output;
            return response;
        }
        catch (Exception ex)
        {
            throw new HttpExceptionWithResponse(500, ex.Message, response);
        }
    }
}