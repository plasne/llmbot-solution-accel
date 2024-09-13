using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Models.Memory;

namespace Inference;

public class ApplyIntent(IWorkflowContext context, ILogger<ApplyIntent> logger)
    : BaseStep<DeterminedIntent, NoOutput?>(logger)
{
    private readonly IWorkflowContext context = context;

    public override string Name => "ApplyIntent";

    public override async Task<NoOutput?> ExecuteInternal(DeterminedIntent input, CancellationToken cancellationToken = default)
    {
        switch (input.Intent)
        {
            case Intents.GREETING:
                await this.context.Stream("Applying intent...", intent: Intents.GREETING);
                this.Continue = false;
                break;
            case Intents.GOODBYE:
                await this.context.Stream("Applying intent...", intent: Intents.GOODBYE);
                this.Continue = false;
                break;
            case Intents.IN_DOMAIN:
                // NOTE: a status is not set here so the intent is just sent with the next dispatch instead of now
                await this.context.Stream(intent: Intents.IN_DOMAIN);
                break;
            case Intents.OUT_OF_DOMAIN:
                await this.context.Stream("Applying intent...", intent: Intents.OUT_OF_DOMAIN);
                this.Continue = !this.context.Config.EXIT_WHEN_OUT_OF_DOMAIN;
                break;
            case Intents.TOPIC_CHANGE:
                await this.context.Stream("Applying intent...", message: input.Query, intent: Intents.TOPIC_CHANGE);
                this.Continue = false;
                break;
            default:
                await this.context.Stream("Applying intent...", intent: Intents.UNKNOWN);
                this.Continue = false;
                break;
        }

        return null;
    }
}