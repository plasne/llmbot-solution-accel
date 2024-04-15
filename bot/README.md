# WeatherBot

This project is just a simple bot that gets a weather forecast streamed from an LLM that will make up fictional weather data.

## Config

Create a .env file in the root of the project (or create Environment Variables some other way) with at least the following content:

```bash
MicrosoftAppType=MultiTenant
MicrosoftAppId=???
MicrosoftAppPassword=???
```

The settings available are:

- __PORT__ [INTEGER, DEFAULT: 3978]: The port the bot will listen on.

- __MicrosoftAppType__ [ONE-OF: MultiTenant, SingleTenent, ManagedIdentity, REQUIRED]: The type of the Microsoft App. Only `MultiTenant` has been tested, but perhaps `SingleTenant` would be suitable as well. When deploying in production, also consider `ManagedIdentity`.

- __MicrosoftAppId__ [GUID, REQUIRED]: The App ID of the Microsoft App. This is the ID of the App Registration in Azure AD.

- __MicrosoftAppPassword__ [STRING, REQUIRED]: The password of the Microsoft App. This is the password of the App Registration in Azure AD.

- __LLM_URI__ [STRING, DEFAULT: http://localhost:5210]: The URI of the LLM API.
