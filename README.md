# Bot Framework LLM Solution Accelerator

This project provides you a simple starting point for a Bot Framework solution that uses an LLM to chat with the user.

This includes streaming content to Teams.

Please see the README.MD files in each of these services to get up and going:

- [bot](./bot/README.md)
- [sk](./sk/README.md)

## TODO

- Add Stylecop
- Hyperlinks for citations​
- Stop and delete (Event Hub)​
- Pluggable history backend​
  - Role, message, feedback, status (edited/deleted/etc.), telemetry, audit, etc.
- Change thumbs-up/down card to highlight option and open further feedback
- Add status when looking up
- Test in Azure + Teams
- Allow for interruptions: <https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-handle-user-interrupt?view=azure-bot-service-4.0&tabs=csharp>

## Interesting Stuff

- Could support different methods of communicating on different channels (turnContext.Activity.ChannelId).

- Proactive: <https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0&tabs=csharp>

## Telemetry

- BOT: TRACEABILITY
- BOT: time to first token
- BOT: time to last token (total response time)
- LLM: total tokens
- LLM: tokens per second
- LLM: time per step
