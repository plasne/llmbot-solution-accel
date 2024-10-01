# Chat Bot

This project is just a simple chat bot that gets messages streamed from an LLM component.

## Azure Bot Service

Deploy an Azure Bot Service and make note of the CLIENT_ID and CLIENT_SECRET for use below.

## Config

Create a .env file in the root of the project (or create Environment Variables some other way) with at least the following content:

```bash
MicrosoftAppType=MultiTenant
MicrosoftAppId=???
MicrosoftAppPassword=???
OPEN_TELEMETRY_CONNECTION_STRING=???
```

The settings available are:

- __PORT__ [INTEGER, DEFAULT: 3978]: The port the bot will listen on.

- __OPEN_TELEMETRY_CONNECTION_STRING__ [STRING, REQUIRED]: The connection string for Open Telemetry. This is used to send telemetry data to Azure Monitor.

- __MicrosoftAppType__ [ONE-OF: MultiTenant, SingleTenent, ManagedIdentity, REQUIRED]: The type of the Microsoft App. Only `MultiTenant` has been tested, but perhaps `SingleTenant` would be suitable as well. When deploying in production, also consider `ManagedIdentity`.

- __MicrosoftAppId__ [GUID, REQUIRED]: The App ID of the Microsoft App. This is the ID of the App Registration in Azure AD. This is also known as the CLIENT_ID.

- __MicrosoftAppPassword__ [STRING, REQUIRED]: The password of the Microsoft App. This is the password of the App Registration in Azure AD. This is also known as the CLIENT_SECRET.

- __MEMORY_URI__ [STRING, DEFAULT: http://localhost:7010]: The URI of the memory service.

- __INFERENCE_URI__ [STRING, DEFAULT: http://localhost:7020]: The URI of the inference service.

- __CHARACTERS_PER_UPDATE__ [INTEGER, DEFAULT: 200]: The number of characters to send to the inference service at a time.

- __FINAL_STATUS__ [STRING, DEFAULT: "Generated."]: The status of a message that is the final message in a conversation.

- __MAX_RETRY_ATTEMPTS__ [INTEGER, DEFAULT: 3]: The maximum number of times to retry a request.

- __SECONDS_BETWEEN_RETRIES__ [INTEGER, DEFAULT: 2]: The number of seconds to wait between retries.

- __MAX_TIMEOUT_IN_SECONDS__ [INTEGER, DEFAULT: 60]: The maximum number of seconds to wait for a response.

- __MAX_PAYLOAD_SIZE__ [INTEGER, DEFAULT: 36864]: The maximum payload size for the adaptive card. The default size comes from <https://learn.microsoft.com/en-us/microsoftteams/platform/bots/how-to/format-your-bot-messages>, but is less to account for metadata in the bot message.

- __VALID_TENANTS__ [ARRAY OF STRINGS, OPTIONAL]: The list of valid tenants. If this is empty, all tenants are valid.

## Building a container

You can build a container by going up a level in the folder structure and running the following:

```bash
docker build -t repo.azurecr.io/memory:2.5.0 -f memory.Dockerfile --platform linux/amd64 .
```

## TODO

- Implement editing (OnMessageUpdateActivityAsync) and deleting (OnMessageDeleteActivityAsync) user messages.
- Develop the user's welcome experience (OnMembersAddedAsync).
- Develop the exception handling (AdapterWithErrorHandler).
- Add a very basic HTTP endpoint (localhost only) for debugging the bot without registering with Bot Service.
- Unit testing/integration testing.
