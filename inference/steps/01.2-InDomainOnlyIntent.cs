using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using Shared;
using Shared.Models.Memory;

namespace Inference;

public class InDomainOnlyIntent(ILogger<InDomainOnlyIntent> logger)
    : BaseStep<WorkflowRequest, DeterminedIntent>(logger), IDetermineIntent
{
    public override string Name => "InDomainOnlyIntent";

    public override Task<DeterminedIntent> ExecuteInternal(
        WorkflowRequest input,
        CancellationToken cancellationToken = default)
    {
        // validate input
        if (string.IsNullOrEmpty(input?.UserQuery))
        {
            throw new HttpException(400, "user_query is required.");
        }

        // return the intent
        var intent = new DeterminedIntent
        {
            Intent = Intents.IN_DOMAIN,
            Query = input.UserQuery,
            SearchQueries = [input.UserQuery],
        };
        return Task.FromResult(intent);
    }
}