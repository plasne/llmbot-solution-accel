using System;
using System.Collections.Generic;
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

    public async Task<WorkflowResponse> Execute(GroundingData groundingData)
    {
        using var activity = DiagnosticService.Source.StartActivity("Workflow");
        var response = new WorkflowResponse();
        try
        {
            // STEP 1: determine intent
            var step1 = new WorkflowStepResponse<GroundingData, Intent>("DetermineIntent", groundingData, this.determineIntent.Logs);
            response.Steps.Add(step1);
            step1.Output = await this.determineIntent.Execute(groundingData);

            // STEP 2: get documents
            var step2 = new WorkflowStepResponse<Intent, List<Doc>>("GetDocuments", step1.Output, this.getDocuments.Logs);
            response.Steps.Add(step2);
            step2.Output = await this.getDocuments.Execute(step1.Output);

            // STEP 3: select grounding data
            var step3 = new WorkflowStepResponse<List<Doc>, GroundingData>("SelectGroundingData", step2.Output, this.selectGroundingData.Logs);

            response.Steps.Add(step3);
            var step3Input = new GroundingData { Docs = step2.Output, History = groundingData.History };
            step3.Output = await this.selectGroundingData.Execute(step3Input);

            // STEP 4: generate answer
            var step4 = new WorkflowStepResponse<GroundingData, string>("GenerateAnswer", step3.Output, this.generateAnswer.Logs);
            response.Steps.Add(step4);
            var step4Input = new IntentAndData { Intent = step1.Output, Data = step3.Output };
            step4.Output = await this.generateAnswer.Execute(step4Input);

            response.Answer = step4.Output;
            return response;
        }
        catch (Exception ex)
        {
            throw new HttpExceptionWithResponse(500, ex.Message, response);
        }
    }
}