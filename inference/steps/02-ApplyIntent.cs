using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Models.Memory;

namespace Inference;

public class ApplyIntent(IWorkflowContext context, ILogger<ApplyIntent> logger)
    : BaseStep<DeterminedIntent, AppliedIntent>(logger)
{
    private readonly IWorkflowContext context = context;

    public override string Name => "ApplyIntent";

    public override async Task<AppliedIntent> ExecuteInternal(DeterminedIntent input, CancellationToken cancellationToken = default)
    {
        switch (input.Intent)
        {
            case Intents.GREETING:
                await this.context.Stream("Applying intent...", intent: Intents.GREETING);
                return new AppliedIntent { Continue = false };
            case Intents.GOODBYE:
                await this.context.Stream("Applying intent...", intent: Intents.GOODBYE);
                return new AppliedIntent { Continue = false };
            case Intents.IN_DOMAIN:
                // NOTE: a status is not set here so the intent is just sent with the next dispatch instead of now
                await this.context.Stream(intent: Intents.IN_DOMAIN);
                return new AppliedIntent { Continue = true };
            case Intents.OUT_OF_DOMAIN:
                await this.context.Stream("Applying intent...", intent: Intents.OUT_OF_DOMAIN);
                return new AppliedIntent { Continue = false };
            case Intents.TOPIC_CHANGE:
                await this.context.Stream("Applying intent...", message: input.Query, intent: Intents.TOPIC_CHANGE);
                return new AppliedIntent { Continue = false };
            default:
                await this.context.Stream("Applying intent...", intent: Intents.UNKNOWN);
                return new AppliedIntent { Continue = false };
        }
    }
}