using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

public class HistoryCommands : ICommands
{
    public Dictionary<string, string> Commands => new()
    {
        { "/new", "starts a new conversation (your chat history will no longer be considered)." },
        { "/stop", "instructs the bot to stop responding." },
        { "/delete", "instructs the bot to stop (if necessary) and delete it's last response." },
        { "/delete #", "instructs the bot to delete from it's history the specified number of exchanges (your messages and the bots responses)." },
    };


    public Task<bool> Try(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default)
    {
        // TODO: write these after the history service
        return Task.FromResult(false);
    }
}