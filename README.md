# Bot Framework LLM Solution Accelerator

This project provides you a simple starting point for a Bot Framework solution that uses an LLM to chat with the user.

This includes streaming content to Teams.

Please see the README.MD files in each of these services to get up and going:

- [bot](./bot/README.md)
- [sk](./sk/README.md)

## TODO

- Add Stylecop
- Explicit topic change (/new)
- Implicit topic change
- `/rate up` and `/rate down` commands
- `/comment ???` to give feedback on the last exchange
- `/feedback` to show the feedback and rating
- `/revoke` to delete the feedback
- `/stop` to stop the current generation.
- `/delete` to delete the most recent generation.
- `/delete #` to delete # turns.
- `/help`
- Stop and delete (Event Hub)​
- Pluggable history backend​
  - Role, message, feedback, status (edited/deleted/etc.), telemetry, audit, etc.
- Implement editing messages
- Prevent multiple responses at the same time.
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
