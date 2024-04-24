using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

public class HelpCommand(ICardProvider cardProvider) : ICommand
{
    private readonly ICardProvider cardProvider = cardProvider;

    public async Task<bool> Try(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default)
    {
        if (turnContext.Activity.Text is null || !turnContext.Activity.Text.Equals("/help", System.StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        await ShowHelp(turnContext, cancellationToken);
        return true;
    }

    public async Task ShowHelp(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default)
    {
        var template = await cardProvider.GetTemplate("help");
        var data = new
        {
            lines = new[]
            {
                new { title = "/help", desc = "gives you information about what the bot can do." },
                new { title = "/new", desc = "starts a new conversation (your chat history will no longer be considered)." },
                new { title = "/feedback", desc = "shows your rating and comments for the last response from the bot." },
                new { title = "/rate up", desc = "rates the last response from the bot as positive." },
                new { title = "/rate down", desc = "rates the last response from the bot as negative." },
                new { title = "/comment ???", desc = "allows you to give feedback comments on the last response from the bot." },
                new { title = "/revoke", desc = "to clear your rating and comments for the last response from the bot." },
                new { title = "/stop", desc = "instructs the bot to stop responding." },
                new { title = "/delete", desc = "instructs the bot to stop (if necessary) and delete it's last response." },
                new { title = "/delete #", desc = "instructs the bot to delete from it's history the specified number of exchanges (your messages and the bots responses)." }
            }
        };
        var attachment = new Attachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = JsonConvert.DeserializeObject(template.Expand(data)),
        };
        var activity = MessageFactory.Attachment(attachment);

        await turnContext.SendActivityAsync(activity, cancellationToken);
    }
}