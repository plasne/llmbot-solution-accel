# LLM

This LLM will provide fictional weather forecast data and stream it to a Bot Framework endpoint using gRPC.

## Config

Create a .env file in the root of the project (or create Environment Variables some other way) with at least the following content:

```bash
LLM_DEPLOYMENT_NAME=MultiTenant
LLM_ENDPOINT_URI=???
LLM_API_KEY=???
OPEN_TELEMETRY_CONNECTION_STRING=???
```

The settings available are:

- __PORT__ [INTEGER, DEFAULT: 3978]: The port the bot will listen on.

- __LLM_DEPLOYMENT_NAME__ [STRING, REQUIRED]: The name of the deployment.

- __LLM_ENDPOINT_URI__ [STRING, REQUIRED]: The endpoint of the LLM deployment.

- __LLM_API_KEY__ [STRING, REQUIRED]: The key to access the LLM.

## TODO

- Add authentication to the gRPC endpoint (mTLS, service account + TLS).
- Add configuration details to the /workflow endpoint.
- Extend HTTP workflow response to make it easier for evaluation to consume.
- Implement retry when DetermineIntent fails to deserialize.
- Make OPEN_TELEMETRY_CONNECTION_STRING optional.
- Convert to Fabrikam bike shop
  - including in-memory search capability - abstract search to interface.
  - including in-memory fake LLM responses.
- Verify that the swagger definition is correct.
- Implement MAX_SEARCH_QUERIES, MAX_CONCURRENT_SEARCHES, SEARCH_STRATEGY, MAX_DOCS_PER_QUERY, SEARCH_MIN_RELEVANCE_SCORE.
- Build http file to demonstrate the use of the API.
- Incorporate custom instructions into the GenerateAnswer step.
- Add token usage to the output of each step that deals with tokens.
- Add Dockerfile.
- Documentation.
- Unit testing/integration testing.
