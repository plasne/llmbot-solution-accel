using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using Shared;
using Shared.Models.Memory;
using Newtonsoft.Json;

namespace Inference;

public class InDomainOnlyIntent(ILogger<InDomainOnlyIntent> logger)
    : BaseStep<WorkflowRequest, DeterminedIntent>(logger), IDetermineIntent
{
    private readonly ILogger<InDomainOnlyIntent> logger = logger;
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

        // if in debug mode, log the intent
#pragma warning disable CA2254 // The logging message template should not vary between calls
        this.logger.LogDebug(JsonConvert.SerializeObject(intent));
#pragma warning restore CA2254 // Restore the warning after this line

        return Task.FromResult(intent);
    }
}