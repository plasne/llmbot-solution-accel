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

- __LLM_URI__ [STRING, DEFAULT: http://localhost:5210]: The URI of the LLM API.

        this.config.Require("PORT", PORT);
        this.config.Require("OPEN_TELEMETRY_CONNECTION_STRING", OPEN_TELEMETRY_CONNECTION_STRING);
        this.config.Require("LLM_URI", this.LLM_URI);
        this.config.Require("CHARACTERS_PER_UPDATE", this.CHARACTERS_PER_UPDATE);
        this.config.Require("MicrosoftAppType");
        this.config.Require("MicrosoftAppId");
        this.config.Require("MicrosoftAppPassword", hideValue: true);
