using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

public interface ICommand
{
    Task<bool> Try(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default);
}