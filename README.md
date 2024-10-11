# Bot Framework LLM Solution Accelerator

This project provides you a simple starting point for a Bot Framework solution that allows users to communicate with a bot over Teams and get answers from an LLM.

There are 3 components:

- [bot](./bot/README.md): A component responsible for communication to and from Teams (or other Bot Framework supported channels). This supports streaming and adaptive cards.

- [memory](./memory/README.md): An API that provides endpoints for storing and retrieving conversation turns and user's custom instructions. It also stores user feedback and basic telemetry. While not currently implemented, it would be possible to extend this service to support data mining use-cases as well.

- [inference](./inference/README.md): A workflow engine that takes a user's input and returns a response using a search services (such as Azure AI Search) and LLMs (such as GPT 4o-mini).

## Getting Started

This accelerator is designed to get up and going with as little configuration as possible. You can run the memory and inference components with no configuration. The bot component requires provisioning an Azure Bot with a Teams channel. With that one configuration, you can have a fully functioning bot that can communicate with users over Teams.

There is a getting started video here: <https://youtu.be/60X0qeFEhXw>.

Without configuration, the memory component will use an in-memory store for conversation turns. Later, you can configure a SQL Server for durable storage.

Without configuration, the inference component will use a simple keyword solution to determine intent, search an in-memory bicycle shop database, and return citations only. Later, you can configure an LLM to determine intent and generate answers. You can configure Azure AI Search instead of the hardcoded bicycle shop data.

To get started, in one terminal window, launch the memory component:

```bash
cd memory
dotnet run
```

In another terminal window, launch the inference component:

```bash
cd inference
dotnet run
```

You can use the inference solution by writing like so:

```bash
curl -X POST http://localhost:7030/api/workflows/primary \
     -H "Content-Type: application/json" \
     -d '{"user_query": "When was the bicycle shop founded?"}'
```

Finally, if you want to test the bot locally, you can create an Azure Bot, add the Teams channel, and make note of the Client ID and Client Secret. Then create a `local.env` file with the following content:

```bash
MicrosoftAppType=MultiTenant (or SingleTenant)
MicrosoftAppId=<Client ID>
MicrosoftAppPassword=<Client Secret>
```

Then launch the bot component in another terminal window:

```bash
cd bot
dotnet run
```

For Teams to be able to talk to the bot, you need to expose the bot to the internet. You can use [ngrok](https://ngrok.com/) for this. Run the following command:

```bash
ngrok http 7000
```

In the Azure Bot configuration in Azure, set the Messaging endpoint to the Forwarding address (appending /api/messages) from ngrok.

![ngrok](./images/ngrok.png)

![config](./images/config.png)

Click on "Open in Teams" in the Teams channel in Azure Bot, and you should be able to chat with the bot directly in Teams.

![channels](./images/channels.png)

Try some of these things:

- hello
- What do you sell?
- What locations do you have?
- new topic. do you sell helmets?
- goodbye

To configure memory to use a SQL Server, you must set at least the following environment variable:

- SQL_SERVER_HISTORY_SERVICE_CONNSTRING

To configure inference to use an LLM, you must set at least the following environment variables:

- LLM_CONNECTION_STRINGS
- LLM_MODEL_NAME

To configure inference to use Azure AI Search, you must set at least the following environment variables:

- SEARCH_MODE=Keyword
- SEARCH_ENDPOINT_URI
- SEARCH_INDEX
- SEARCH_API_KEY

## More Info

These components can be used together or separately. For more information on each component or to see the many configuration items, see the READMEs in their respective folders.

## Interesting Stuff

- Could support different methods of communicating on different channels (turnContext.Activity.ChannelId).

- Proactive: <https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0&tabs=csharp>

- Allow for interruptions: <https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-handle-user-interrupt?view=azure-bot-service-4.0&tabs=csharp>
