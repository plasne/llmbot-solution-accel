using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

public class FeedbackCommand(ILogger<FeedbackCommand> logger) : ICommand
{
    private readonly ILogger<FeedbackCommand> logger = logger;

    public async Task<bool> Try(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default)
    {
        var userId = turnContext.Activity.From.AadObjectId;

        // look for actions
        var jaction = turnContext.Activity.Value as JObject;
        var action = jaction?.ToObject<UserAction>();
        if (action is not null && !string.IsNullOrEmpty(action.ChatId))
        {
            switch (action.Command)
            {
                case "/rate up":
                    {
                        await this.Rate(userId, action.ChatId, "up", turnContext, cancellationToken);
                        break;
                    }
                case "/rate down":
                    {
                        await this.Rate(userId, action.ChatId, "down", turnContext, cancellationToken);
                        break;
                    }
            }
        }

        // look for commands
        switch (turnContext.Activity.Text?.ToLower())
        {
            case "/rate":
                {
                    await ShowRatings(turnContext, cancellationToken);
                    break;
                }
            case "/rate up":
                {
                    var chatId = await this.GetLastChatId();
                    await this.Rate(userId, chatId, "up", turnContext, cancellationToken);
                    break;
                }
            case "/rate down":
                {
                    var chatId = await this.GetLastChatId();
                    await this.Rate(userId, chatId, "down", turnContext, cancellationToken);
                    break;
                }
        }

        return false;
    }

    private Task<string> GetLastChatId()
    {
        throw new NotImplementedException();
    }

    private async Task Rate(string userId, string chatId, string value, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("User {user} rated {id} as {value}", userId, chatId, value);
        var activity = MessageFactory.Text($"Thank you for rating '{value}' on chat '{chatId}'.");
        await turnContext.SendActivityAsync(activity, cancellationToken);
    }

    private async Task ShowRatings(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var chatId = await this.GetLastChatId();
        throw new NotImplementedException();
    }
}