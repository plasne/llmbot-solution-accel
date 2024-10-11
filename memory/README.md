# Memory

The memory service is responsible for managing conversation turns and user's custom instructions. It could also be extended in the future for other memory capabilities.

## Config

Create a "local.env" file in the root of the project (or create Environment Variables some other way). The settings available are:

__ENV_FILES__ [STRING, OPTIONAL]: A comma-delimited list of files to load. The files are loaded in order, so later files take precedence. The default is "local.env".

- __PORT__ [INTEGER, DEFAULT: 7010]: The port the web server will listen on. This is used by developers for local testing and by evaluation.

- __OPEN_TELEMETRY_CONNECTION_STRING__ [STRING, OPTIONAL]: The connection string for the OpenTelemetry exporter (AppInsights). Rather than provide a string, you can provide a Key Vault URL to a secret.

- __SQL_SERVER_MAX_RETRY_ATTEMPTS__ [INT, OPTIONAL, DEFAULT: 3]: Max retry attempts to connect to SQL server.

- __SQL_SERVER_SECONDS_BETWEEN_RETRIES__ [STRING, OPTIONAL, DEFAULT: 2]: The number of seconds to wait between each retry attempt.

- __SQL_SERVER_HISTORY_SERVICE_CONNSTRING__ [STRING, OPTIONAL]: The SQL server connection string. Rather than provide a string, you can provide a Key Vault URL to a secret. Connection strings without a password (using DefaultAzureCredential) is also supported.

- __DEFAULT_RETENTION__ [DURATION, DEFAULT: "P3M" (3 months)]: The default retention period (in ISO 8601 format) for conversation turns.

- __RUN_RETENTION_EVERY_X_HOURS__ [int, DEFAULT: 8]: The number of hours between each job to clean up records beyond their expiry date.

In addition to those settings, there are some settings that are available as part of the NetBricks integration, including:

- __LOG_LEVEL__ [STRING, DEFAULT: "Information"]: The log level for the application. This can be set to "None", "Trace", "Debug", "Information", "Warning", "Error", or "Critical".

- __DISABLE_COLORS__ [STRING, DEFAULT: false]: If true, colors will be disabled in the logs. This is helpful for many logging systems other than the console.

- __APPCONFIG_URL__ [STRING, OPTIONAL]: The URL for the App Configuration service. This is used to pull settings from Azure App Configuration.

- __CONFIG_KEYS__ [STRING, OPTIONAL]: This is a comma-delimited list of configuration keys to pull for the specific service. All keys matching the pattern will be pulled. A setting that is already set is not replaced (so left-most patterns take precident). For example, the dev environment of the auth service might contain "app:auth:dev:*, app:common:dev:*". If you do not specify any CONFIG_KEYS, no variables will be set from App Configuration.

- __ASPNETCORE_ENVIRONMENT__ [STRING, DEFAULT: "Development"]: This is a common .NET setting. It is used by INCLUDE_CREDENTIAL_TYPES.

- __INCLUDE_CREDENTIAL_TYPES__ [STRING, *]: This is a comma-delimited list of credential types to consider when connecting to App Configuration, Key Vault, or using DefaultAzureCredential. It can include "env", "mi", "token", "vs", "vscode", "azcli", and/or "browser". If __ASPNETCORE_ENVIRONMENT__ is "Development", then the default is "azcli, env"; otherwise, the default is "env, mi". You can find out more about the options [here](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet).

## Running locally

To run the solution locally, you need to do the following:

```bash
dotnet run
```

## Building a container

You can build a container by going up a level in the folder structure and running the following:

```bash
docker build -t repo.azurecr.io/memory:2.5.0 -f memory.Dockerfile --platform linux/amd64 .
```

## TODO

- clean up some of the swagger definition that doesn't look right (ex. enums).
- implement retention in SqlServerMaintenanceService.
- Add hash to custom instructions.
- Unit testing/integration testing.
- Documentation.
