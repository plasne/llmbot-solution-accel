# Bot Framework LLM Solution Accelerator

This project provides you a simple starting point for a Bot Framework solution that uses an LLM to chat with the user.

This includes streaming content to Teams.

Please see the README.MD files in each of these services to get up and going:

- [Bot](./bot/README.md)
- [LLM](./llm/README.md)

## TODO

- Add status when looking up
- Implement links
- Test in Azure + Teams
- Allow for interruptions: <https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-handle-user-interrupt?view=azure-bot-service-4.0&tabs=csharp>

- Could support different methods of communicating on different channels (turnContext.Activity.ChannelId).

## Discussion

- Do you want LLM to be .NET and Python?
- OK to use .NET 8?
- Are we going to use separate services or a sidecar?
- Protocol...
  - gRPC
  - web sockets
  - async HTTP
  - Server Sent Events
  - Unix Domain Sockets
- Do we require TLS between those services?
- Are Adaptive Cards too narrow? Should we use one at the beginning and one at the end?
- How should we notify of /stop? (db, event hub)

## Proactive

<https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0&tabs=csharp>

## Telemetry

- BOT: TRACEABILITY
- BOT: time to first token
- BOT: time to last token (total response time)
- LLM: total tokens
- LLM: tokens per second
- LLM: time per step
