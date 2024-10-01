using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using Shared;
using Shared.Models.Memory;
using System;
using Newtonsoft.Json;

namespace Inference;

public class DetermineIntentWithKeywords(ILogger<DetermineIntentWithKeywords> logger)
    : BaseStep<WorkflowRequest, DeterminedIntent>(logger), IDetermineIntent
{
    private readonly ILogger<DetermineIntentWithKeywords> logger = logger;
    public override string Name => "DetermineIntentWithKeywords";

    public override Task<DeterminedIntent> ExecuteInternal(
        WorkflowRequest input,
        CancellationToken cancellationToken = default)
    {
        // validate input
        if (string.IsNullOrEmpty(input?.UserQuery))
        {
            throw new HttpException(400, "user_query is required.");
        }

        // assume IN_DOMAIN
        var intent = new DeterminedIntent
        {
            Intent = Intents.IN_DOMAIN,
            Query = input.UserQuery,
        };

        // see if keywords, change it to something else
        if (input.UserQuery.StartsWith("new topic", StringComparison.InvariantCultureIgnoreCase))
        {
            intent.Intent = Intents.TOPIC_CHANGE;
            var split = input.UserQuery.Split(".");
            if (split.Length == 2)
            {
                intent.Query = split[1];
            }
            else
            {
                intent.Query = "";
            }
        }
        else if (input.UserQuery.StartsWith("goodbye", StringComparison.InvariantCultureIgnoreCase))
        {
            intent.Intent = Intents.GOODBYE;
        }
        else if (input.UserQuery.StartsWith("hello", StringComparison.InvariantCultureIgnoreCase))
        {
            intent.Intent = Intents.GREETING;
        }

        // if in debug mode, log the intent
#pragma warning disable CA2254 // The logging message template should not vary between calls
        this.logger.LogDebug(JsonConvert.SerializeObject(intent));
#pragma warning restore CA2254 // Restore the warning after this line

        // return the intent
        return Task.FromResult(intent);
    }
}