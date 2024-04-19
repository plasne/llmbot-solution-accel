using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class Workflow()
{
    public async Task<WorkflowResponse> Execute(IServiceScope scope, GroundingData groundingData)
    {
        var response = new WorkflowResponse();

        // STEP 1: determine intent
        var determineIntent = scope.ServiceProvider.GetRequiredService<DetermineIntent>();
        var intent = await determineIntent.Execute(groundingData);
        response.Steps.Add(new WorkflowStepResponse<GroundingData, Intent>("DetermineIntent", groundingData, intent, determineIntent.Logs));

        // STEP 2: get documents
        var getDocuments = scope.ServiceProvider.GetRequiredService<GetDocuments>();
        var docs = await getDocuments.Execute(intent);
        response.Steps.Add(new WorkflowStepResponse<Intent, List<Doc>>("GetDocuments", intent, docs, getDocuments.Logs));

        // STEP 3: select grounding data
        var selectGroundingData = scope.ServiceProvider.GetRequiredService<SelectGroundingData>();
        var selectGroundingDataInput = new GroundingData { Docs = docs, History = groundingData.History };
        var selectedGroundingData = await selectGroundingData.Execute(selectGroundingDataInput);
        response.Steps.Add(new WorkflowStepResponse<GroundingData, GroundingData>("SelectGroundingData", groundingData, selectedGroundingData, selectGroundingData.Logs));

        // STEP 4: generate answer
        var generateAnswer = scope.ServiceProvider.GetRequiredService<GenerateAnswer>();
        var generateAnswerInput = new IntentAndData { Intent = intent, Data = selectedGroundingData };
        var answer = await generateAnswer.Execute(generateAnswerInput);
        response.Steps.Add(new WorkflowStepResponse<IntentAndData, string>("GenerateAnswer", generateAnswerInput, answer, generateAnswer.Logs));

        response.Answer = answer;
        return response;
    }
}