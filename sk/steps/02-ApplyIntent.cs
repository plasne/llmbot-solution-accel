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
                await this.context.Stream(intent: DistributedChat.Intent.Greeting);
                return new AppliedIntent { Continue = false };
            case Intents.GOODBYE:
                await this.context.Stream(intent: DistributedChat.Intent.Goodbye);
                return new AppliedIntent { Continue = false };
            case Intents.IN_DOMAIN:
                await this.context.Stream(intent: DistributedChat.Intent.InDomain);
                return new AppliedIntent { Continue = true };
            case Intents.OUT_OF_DOMAIN:
                await this.context.Stream(intent: DistributedChat.Intent.OutOfDomain);
                return new AppliedIntent { Continue = false };
            case Intents.TOPIC_CHANGE:
                await this.context.Stream(message: input.Query, intent: DistributedChat.Intent.TopicChange);
                return new AppliedIntent { Continue = false };
            default:
                await this.context.Stream(intent: DistributedChat.Intent.Unknown);
                return new AppliedIntent { Continue = false };
        }
    }
}