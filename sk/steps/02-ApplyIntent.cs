using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class ApplyIntent(IContext context, ILogger<ApplyIntent> logger) : BaseStep<DeterminedIntent, AppliedIntent>(logger)
{
    private readonly IContext context = context;

    public override string Name => "ApplyIntent";

    public override async Task<AppliedIntent> ExecuteInternal(DeterminedIntent input, CancellationToken cancellationToken = default)
    {
        switch (input.Intent)
        {
            case Intents.GREETING:
                await this.context.Terminate(DistributedChat.Intent.Greeting);
                return new AppliedIntent { Continue = false };
            case Intents.GOODBYE:
                await this.context.Terminate(DistributedChat.Intent.Goodbye);
                return new AppliedIntent { Continue = false };
            case Intents.IN_DOMAIN:
                // in-domain questions are handled by the next step
                return new AppliedIntent { Continue = true };
            case Intents.OUT_OF_DOMAIN:
                await this.context.Terminate(DistributedChat.Intent.OutOfDomain);
                return new AppliedIntent { Continue = false };
            case Intents.TOPIC_CHANGE:
                await this.context.Terminate(DistributedChat.Intent.TopicChange, input.Query);
                return new AppliedIntent { Continue = false };
            default:
                await this.context.Terminate(DistributedChat.Intent.Unknown);
                return new AppliedIntent { Continue = false };
        }
    }
}