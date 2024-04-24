using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

public class HelpCommand(IServiceProvider serviceProvider, ICardProvider cardProvider) : ICommands
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ICardProvider cardProvider = cardProvider;

    public Dictionary<string, string> Commands => new()
    {
        { "/help", "gives you information about what the bot can do." },
    };

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
        var commands = this.serviceProvider.GetServices<ICommands>();
        var data = new
        {
            commands = commands
                .SelectMany(c => c.Commands)
                .Select(cmd => new { title = cmd.Key, desc = cmd.Value })
                .ToArray()
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