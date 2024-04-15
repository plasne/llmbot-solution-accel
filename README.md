# README

## TODO

- Add settings
- Write docs
- Test in Azure + Teams
- Build out the sample to more accurately reflect chatting
- Include in-memory history
- Allow for interruptions: <https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-handle-user-interrupt?view=azure-bot-service-4.0&tabs=csharp>
- Clean all the junk out of bot sample

- Could support different methods of communicating on different channels (turnContext.Activity.ChannelId).

## Discussion

- Is it maybe better to use a Unix Domain Socket instead of gRPC? I am just wondering because of the possible requirement for encryption.
- There are a lot more than just Adaptive Cards (Hero Cards, Actions on Messages, Animation Cards, Audio Cards, Recipient Cards, Signin Cards, etc.)
- Are Adaptive Cards too narrow? Should we use one at the beginning and one at the end?
- How should we notify of /stop? (db, event hub)

## Proactive

<https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0&tabs=csharp>

## Config

In bot create the following .env:

```bash
MicrosoftAppType=MultiTenant
MicrosoftAppId=???
MicrosoftAppPassword=???
```

In llm create the following .env:

```bash
LLM_DEPLOYMENT_NAME=???
LLM_ENDPOINT_URI=https://???.openai.azure.com
LLM_API_KEY=???
```
