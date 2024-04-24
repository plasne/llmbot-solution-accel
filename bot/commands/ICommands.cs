using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

public interface ICommands
{
    Dictionary<string, string> Commands { get; }

    Task<bool> Try(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default);
}