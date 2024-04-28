using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

public class HistoryCommands(IConfig config, IHistoryService historyService) : ICommands
{
    private readonly IConfig config = config;
    private readonly IHistoryService historyService = historyService;

    public Dictionary<string, string> Commands => new()
    {
        { "/new", "starts a new conversation (your chat history will no longer be considered)." },
        { "/stop", "instructs the bot to stop responding." },
        { "/delete", "instructs the bot to stop (if necessary) and delete it's last response." },
        { "/delete #", "instructs the bot to delete from it's history the specified number of exchanges (your messages and the bots responses)." },
        { "/delete all", "instructs the bot to delete all exchanges from history." }
    };


    public async Task<bool> Try(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default)
    {
        // TODO: write these after the history service
        if (turnContext.Activity.Text == "/new")
        {
            // write a topic change to the history service
            var userId = turnContext.Activity.From.AadObjectId;
            var changeTopic = Interaction.CreateTopicChange(turnContext.Activity.Id, userId);
            await this.historyService.ChangeConversationTopicAsync(changeTopic, this.config.DEFAULT_RETENTION);

            // confirm the topic change to the user
            var activity = MessageFactory.Text("Let's start a new conversation.");
            await turnContext.SendActivityAsync(activity, cancellationToken);

            return true;
        }

        return false;
    }
}