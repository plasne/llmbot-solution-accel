# Bot Framework LLM Solution Accelerator

This project provides you a simple starting point for a Bot Framework solution that uses an LLM to chat with the user.

This includes streaming content to Teams.

Please see the README.MD files in each of these services to get up and going:

- [Bot](./bot/README.md)
- [LLM](./llm/README.md)

## TODO

- Test in Azure + Teams
- Build out the sample to more accurately reflect chatting
- Include in-memory history
- Allow for interruptions: <https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-handle-user-interrupt?view=azure-bot-service-4.0&tabs=csharp>

- Could support different methods of communicating on different channels (turnContext.Activity.ChannelId).

## Discussion

- Is it maybe better to use a Unix Domain Socket instead of gRPC? I am just wondering because of the possible requirement for encryption.
- There are a lot more than just Adaptive Cards (Hero Cards, Actions on Messages, Animation Cards, Audio Cards, Recipient Cards, Signin Cards, etc.)
- Are Adaptive Cards too narrow? Should we use one at the beginning and one at the end?
- How should we notify of /stop? (db, event hub)

## Proactive

<https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0&tabs=csharp>
