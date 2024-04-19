using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class Workflow()
{
    public async Task<string> Execute(IServiceScope scope, GroundingData groundingData)
    {
        // STEP 1: determine intent
        var determineIntent = scope.ServiceProvider.GetRequiredService<DetermineIntent>();
        var intent = await determineIntent.Execute(groundingData);

        // STEP 2: get documents
        var getDocuments = scope.ServiceProvider.GetRequiredService<GetDocuments>();
        var docs = await getDocuments.Execute(intent);

        // STEP 3: select grounding data
        var selectGroundingData = scope.ServiceProvider.GetRequiredService<SelectGroundingData>();
        var selectGroundingDataInput = new GroundingData { Docs = docs, History = groundingData.History };
        var selectedGroundingData = await selectGroundingData.Execute(selectGroundingDataInput);

        // STEP 4: generate answer
        var generateAnswer = scope.ServiceProvider.GetRequiredService<GenerateAnswer>();
        var generateAnswerInput = new IntentAndData { Intent = intent, Data = selectedGroundingData };
        var answer = await generateAnswer.Execute(generateAnswerInput);

        return answer;
    }
}