using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Bots;

public class WeatherBot : ActivityHandler
{
    private readonly WeatherChannel weatherChannel;
    private readonly string cardJson;

    public WeatherBot(WeatherChannel weatherChannel)
    {
        this.weatherChannel = weatherChannel;
        this.cardJson = File.ReadAllText("./card.json");
    }

    private async Task<string> Dispatch(
        string? id,
        string status,
        string text,
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        // var activity = MessageFactory.Text(text, text);

        var valid = JsonConvert.ToString(text);
        var json = cardJson.Replace("${status}", status).Replace("\"${body}\"", valid);
        var attachment = new Attachment()
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JsonConvert.DeserializeObject(json),
        };
        var activity = MessageFactory.Attachment(attachment);

        if (string.IsNullOrEmpty(id))
        {
            var response = await turnContext.SendActivityAsync(activity, cancellationToken);
            return response.Id;
        }

        activity.Id = id;
        await turnContext.UpdateActivityAsync(activity, cancellationToken);
        return id;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        if (turnContext.Activity.Text.StartsWith("/rate"))
        {
            await turnContext.SendActivityAsync("Thanks for rating this response!");
            return;
        }

        await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        using var streamingCall = this.weatherChannel.Client.GetWeatherStream(new Empty(), cancellationToken: cts.Token);
        try
        {
            string? id = null;
            StringBuilder summaries = new();
            int lastSentAtLength = 0;
            await foreach (var weatherData in streamingCall.ResponseStream.ReadAllAsync(cancellationToken: cts.Token))
            {
                summaries.Append(weatherData.Summary);
                if (summaries.Length - lastSentAtLength > 200)
                {
                    lastSentAtLength = summaries.Length;
                    id = await Dispatch(id, "generating...", summaries.ToString(), turnContext, cancellationToken);
                }
            }
            await Dispatch(id, "generated.", summaries.ToString(), turnContext, cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Console.WriteLine("Stream cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeText = "Hello and welcome!";
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
            }
        }
    }
}
