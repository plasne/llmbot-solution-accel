# Memory

The memory service is responsible for managing conversation turns and user's custom instructions. It could also be extended in the future for other memory capabilities.

## Config

The provided .env file should contain all settings required to run.

The settings available are:

- __PORT__ [INTEGER, DEFAULT: 7010]: The port the web server will listen on. This is used by developers for local testing and by evaluation.

- __OPEN_TELEMETRY_CONNECTION_STRING__ [STRING, OPTIONAL]: The connection string for the OpenTelemetry exporter (AppInsights). Rather than provide a string, you can provide a Key Vault URL to a secret.

- __SQL_SERVER_MAX_RETRY_ATTEMPTS__ [INT, OPTIONAL, DEFAULT: 3]: Max retry attempts to connect to SQL server.

- __SQL_SERVER_SECONDS_BETWEEN_RETRIES__ [STRING, OPTIONAL, DEFAULT: 2]: The number of seconds to wait between each retry attempt.

- __SQL_SERVER_HISTORY_SERVICE_CONNSTRING__ [STRING, REQUIRED]: The SQL server connection string. Rather than provide a string, you can provide a Key Vault URL to a secret.

## Running locally

To run the solution locally, you need to do the following:

```bash
dotnet run
```

## TODO

- test all cases for LocalMemoryStore.
- clean up some of the swagger definition that doesn't look right (ex. enums).
- implement retention in SqlServerMaintenanceService.
- Add hash to custom instructions.
- Unit testing/integration testing.
- Documentation.
