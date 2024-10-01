using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using ChangeFeed;
using Microsoft.Bot.Schema;
using Shared.Models.Memory;
using Newtonsoft.Json.Linq;
using Shared.Models;
using Newtonsoft.Json;

namespace Bot;

public class MemoryCommands(IConfig config, IHttpClientFactory httpClientFactory, IChangeFeed changeFeed, StopUserMessageMemory stopUserMessageMemory) : ICommands
{
    private readonly IConfig config = config;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IChangeFeed changeFeed = changeFeed;
    private readonly StopUserMessageMemory stopUserMessageMemory = stopUserMessageMemory;

    public Dictionary<string, string> Commands => new()
    {
        { "/new", "starts a new conversation (your chat history will no longer be considered)." },
        { "/stop", "instructs the bot to stop responding." },
        { "/delete", "instructs the bot to stop (if necessary) and delete it's last response." },
        { "/delete #", "instructs the bot to delete from it's history the specified number of exchanges (your messages and the bots responses)." },
        { "/delete all", "instructs the bot to delete all exchanges from history." },
    };

    public async Task<bool> Try(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken = default)
    {
        if (turnContext.Activity.Text == "/new")
        {
            // write a topic change to the history service
            using var httpClient = httpClientFactory.CreateClient("retry");
            var userId = turnContext.Activity.From.AadObjectId;
            var res = await httpClient.PutAsync(
                $"{config.MEMORY_URL}/api/users/{userId}/conversations",
                new ChangeTopicRequest { Intent = Intents.TOPIC_CHANGE, ActivityId = turnContext.Activity.Id }.ToJsonContent(),
                cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"the attempt to PUT /conversations with TOPIC_CHANGE resulted in HTTP {res.StatusCode} - {content}.");
            }

            // confirm the topic change to the user
            var activity = MessageFactory.Text("Let's start a new conversation.");
            await turnContext.SendActivityAsync(activity, cancellationToken);

            return true;
        }

        if (turnContext.Activity.Value is JObject jaction && jaction.ToObject<UserAction>() is UserAction action && !string.IsNullOrEmpty(action.ActivityId))
        {
            if (action.Command == "/stop")
            {
                if (!stopUserMessageMemory.TryRemove(action.ActivityId))
                {
                    await changeFeed.NotifyAsync($"STOP.{action.ActivityId}", cancellationToken);
                }
                return true;
            }

            if (action.Command == "/delete")
            {
                using var httpClient = httpClientFactory.CreateClient("retry");
                var userId = turnContext.Activity.From.AadObjectId;
                var res = await httpClient.DeleteAsync(
                    $"{config.MEMORY_URL}/api/users/{userId}/activities/{turnContext.Activity.ReplyToId.ToBase64()}",
                    cancellationToken);
                if (!res.IsSuccessStatusCode)
                {
                    var content = await res.Content.ReadAsStringAsync(cancellationToken);
                    throw new Exception($"the attempt to DELETE /activities resulted in HTTP {res.StatusCode} - {content}.");
                }

                IMessageActivity activity = MessageFactory.Text("User has deleted this message.");
                activity.Id = turnContext.Activity.ReplyToId;
                await turnContext.UpdateActivityAsync(activity, cancellationToken);

                return true;
            }
        }

        if (turnContext.Activity.Text.StartsWith("/delete"))
        {
            var deleteCommandParts = turnContext.Activity.Text.Split(' ');
            int deleteCount;
            if (deleteCommandParts.Length == 1)
            {
                deleteCount = 1;
            }
            else if (deleteCommandParts.Length == 2 && deleteCommandParts[1] == "all")
            {
                // it is highly unlikely that we will see the number of messages
                deleteCount = int.MaxValue;
            }
            else if (deleteCommandParts.Length == 2 && int.TryParse(deleteCommandParts[1], out deleteCount))
            {
                // do nothing, deleteCount should be set
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("I am sorry but the delete command is not valid. If you aren't sure what I can do type `/help`."), cancellationToken);
                return true;
            }

            using var httpClient = httpClientFactory.CreateClient("retry");
            var userId = turnContext.Activity.From.AadObjectId;
            var res = await httpClient.DeleteAsync(
                $"{config.MEMORY_URL}/api/users/{userId}/activities/:last?count={deleteCount}",
                cancellationToken);

            var content = await res.Content.ReadAsStringAsync(cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"the attempt to DELETE /activities resulted in HTTP {res.StatusCode} - {content}.");
            }

            var deletedActivities = JsonConvert.DeserializeObject<List<DeletedUserMessage>>(content);
            if (deletedActivities is not null && deletedActivities.Count > 0)
            {
                foreach (var deletedActivity in deletedActivities)
                {
                    if (deletedActivity.Role == Roles.ASSISTANT)
                    {
                        IMessageActivity activity = MessageFactory.Text("User has deleted this message.");
                        activity.Id = deletedActivity.ActivityId;
                        await turnContext.UpdateActivityAsync(activity, cancellationToken);
                    }
                }

                await turnContext.SendActivityAsync(MessageFactory.Text(
                    deletedActivities.Count == 1 ? "1 message was deleted from the bot memory." :
                    $"{deletedActivities.Count} messages were deleted from the bot memory."), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"No message deleted from the bot memory."), cancellationToken);
            }

            return true;
        }

        return false;
    }
}