# LLM

This LLM will provide fictional weather forecast data and stream it to a Bot Framework endpoint using gRPC.

## Config

Create a .env file in the root of the project (or create Environment Variables some other way) with at least the following content:

```bash
LLM_DEPLOYMENT_NAME=MultiTenant
LLM_ENDPOINT_URI=???
LLM_API_KEY=???
```

The settings available are:

- __PORT__ [INTEGER, DEFAULT: 3978]: The port the bot will listen on.

- __LLM_DEPLOYMENT_NAME__ [STRING, REQUIRED]: The name of the deployment.

- __LLM_ENDPOINT_URI__ [STRING, REQUIRED]: The endpoint of the LLM deployment.

- __LLM_API_KEY__ [STRING, REQUIRED]: The key to access the LLM.
