# LLM

This inference solution takes input via gRPC or HTTP and sends it to the LLM for processing. The HTTP interface is only accessible via localhost; it is used for debugging and for evaluation.

## Config

The provided .env file should contain all settings required to run.

The settings available are:

__ENV_FILES__ [STRING, OPTIONAL]: A comma-delimited list of files to load. The files are loaded in order, so later files take precedence. The default is "local.env".

- __GRPC_PORT__ [INTEGER, DEFAULT: 7020]: The port the gRPC server will listen on. This is used by the bot service.

- __WEB_PORT__ [INTEGER, DEFAULT: 7030]: The port the web server will listen on. This is used by developers for local testing and by evaluation.

- __OPEN_TELEMETRY_CONNECTION_STRING__ [STRING, OPTIONAL]: The connection string for the OpenTelemetry exporter (AppInsights). Rather than provide a string, you can provide a Key Vault URL to a secret.

- __MEMORY_TERM__ [STRING, DEFAULT: "Long"]: If long, once a prompt file is cached, it remains cached. If short, the prompt file is only cached for one execution.

- __MEMORY_URL__ [STRING, DEFAULT: "http://localhost:7010"]: The URL for the memory service.

- __MAX_RETRY_ATTEMPTS__ [INTEGER, DEFAULT: 3]: The maximum number of times to retry a request.

- __SECONDS_BETWEEN_RETRIES__ [INTEGER, DEFAULT: 2]: The number of seconds to wait between retries.

- __MAX_TIMEOUT_IN_SECONDS__ [INTEGER, DEFAULT: 60]: The maximum number of seconds to wait for a response.

- __SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT__ [INTEGER, DEFAULT: 14000]: The context window token limit to apply during grounding step for selecting context and history that fits into the limit.

- __EXIT_WHEN_OUT_OF_DOMAIN__ [BOOLEAN, DEFAULT: true]: If true, the service will exit when the DetermineIntent step returns an OUT_OF_DOMAIN intent.

- __EXIT_WHEN_NO_DOCUMENTS__ [BOOLEAN, DEFAULT: true]: If true, the service will exit when the GetDocuments step finds no documents.

- __EXIT_WHEN_NO_CITATIONS__ [BOOLEAN, DEFAULT: false]: If true, the service will exit when the GenerateAnswer step cites no documents.

- __LLM_CONNECTION_STRINGS__ [ARRAY, OPTIONAL]: A list of connection strings separated by ";;". Each connection string will have parts (DeploymentName, Endpoint, and ApiKey) that should be separated by a ";". Rather than provide a string, you can provide a Key Vault URL to a secret. If not provided, the intent will always be IN_DOMAIN and answers will only include citations. If provided, the following are required:

  - __LLM_MODEL_NAME__ [STRING, REQUIRED]: The model name for the LLM. This must be a model name that can be understood by SharpToken. This is used to calculate the number of tokens.

  - __INTENT_PROMPT_FILE__ [STRING, DEFAULT: "./templates/intent-prompt.txt"]: The file used to prompt the LLM for intent.

  - __CHAT_PROMPT_FILE__ [STRING, DEFAULT: "./templates/chat-prompt.txt"]: The file used to prompt the LLM for an answer.

  - __INTENT_TEMPERATURE__ [DECIMAL, DEFAULT: 0.0]: The temperature used when prompting the LLM for intent.

  - __CHAT_TEMPERATURE__ [DECIMAL, DEFAULT: 0.3]: The temperature used when prompting the LLM for an answer.

  - __INTENT_SEED__ [INTEGER, OPTIONAL]: The seed used when prompting the LLM for intent. You can also just provide a value as SEED.

  - __CHAT_SEED__ [INTEGER, OPTIONAL]: The seed used when prompting the LLM for an answer. You can also just provide a value as SEED.

- __SEARCH_ENDPOINT_URI__ [STRING, OPTIONAL]: The endpoint URI for the search service. If not provided, the internal bicycle shop data is used. If provided, the following are required:

  - __SEARCH_INDEX__ [STRING, REQUIRED]: The index to search for similarity searches.

  - __SEARCH_API_KEY__ [STRING, REQUIRED]: The API key for the search service.

  - __SEARCH_MODE__ [STRING, DEFAULT: "HybridWithSemanticRerank"]: The mode for the search service. This can be Keyword, Vector, Hybrid, KeywordWithSemanticRerank, or HybridWithSemanticRerank. If SEARCH_MODE is set to Vector, Hybrid, or HybridWithSemanticRerank, the following are required:

    - __EMBEDDING_CONNECTION_STRINGS__ [ARRAY, OPTIONAL]: A list of connection strings separated by ";;". Each connection string will have parts (DeploymentName, Endpoint, and ApiKey) that should be separated by a ";". Rather than provide a string, you can provide a Key Vault URL to a secret.

    - __EMBEDDING_MODEL_NAME__ [STRING, REQUIRED]: The model name for embedding. This must be a model name that can be understood by SharpToken. This is used to calculate the number of tokens.

    - __SEARCH_VECTOR_FIELDS__ [ARRAY, DEFAULT: "contentVector"]: The vector fields used for similarity searches.

    - __SEARCH_VECTOR_EXHAUST_KNN__ [BOOLEAN, DEFAULT: false]: If true, vector mode will perform a brute-force similarity search that scans the entire vector space using the exhaustive k-nearest neighbors (eKNN) algorithm. Otherwise, it will use the approximate knn HNSW filter at query time.

    - __SEARCH_KNN__ [INTEGER,  DEFAULT: 10]: This is the knn to use for both approximate nearest neighbors (knn) and exhaustive knn (eknn).

    If SEARCH_MODE is set to KeywordWithSemanticRerank or HybridWithSemanticRerank, the following are required:

    - __MIN_RELEVANCE_RERANK_SCORE__ [DECIMAL, DEFAULT: 2]: The minimum relevance reranker score. This is compared against the relevance reranker scores returned from Azure AI search results when semantic rerank feature is enabled, a semantic config is provisioned on the service and it's name is provided as a value to __SEARCH_SEMANTIC_RERANK_CONFIG__.

    - __SEARCH_SEMANTIC_RERANK_CONFIG__ [STRING, Default: "default"]: The name of the semantic configuration for the search service. When using the `KeywordWithSemanticRerank` or `HybridWithSemanticRerank` Search Mode, this configuration will be used to rerank the search results if the rerank feature is enabled on the deployed search service. If you are unsure if a custom semantic configuration exists, visit the service. Please note that this will throw a 400 `Azure.RequestFailedException` and fail all requests if semantic configuration cannot be found. <u> NOTE: Provisioning a semantic config with name that matches the default name on the application (i.e. `default`) is recommended in order to reduce the need to update the application configuration when the semantic config is updated. The semantic ranker can take up to 50 results. </u>

  - __SEARCH_SELECT_FIELDS__ [ARRAY, DEFAULT: ["title", "content", "urls"]]: The fields to select from the search results.

  - __SEARCH_TRANSFORM_FILE__ [STRING, REQUIRED]: The file used to transform search results. The documents in the index may not conform to the Doc file, this is a Jsonata template that will transform the results into the correct format.

  - __MAX_CONCURRENT_SEARCHES__ [INTEGER, DEFAULT: 3]: The maximum number of concurrent searches.

  - __MIN_RELEVANCE_SEARCH_SCORE__ [DECIMAL, DEFAULT: 0.0]: The minimum relevance search score. This is compared against the relevance search scores returned from Azure AI search results and is used to filter out irrelevant search results. Azure AI Search calculates the returned search score results differently based on the type of query. For example, a full text query may return a score of 76.57179, while a semantic query may return a score of 0.71832.

  - __SEARCH_TOP__ [INTEGER, DEFAULT: 10]: The top scoring results to return from the search service per query.

  - __MAX_SEARCH_QUERIES_PER_INTENT__ [INTEGER, DEFAULT: 3]: The maximum number of search queries per intent.

  - __PICK_DOCS_URL_FIELD__ [STRING, REQUIRED]: The field in the search results that contains the URL for the document.

In addition to those settings, there are some settings that are available as part of the NetBricks integration, including:

- __LOG_LEVEL__ [STRING, DEFAULT: "Information"]: The log level for the application. This can be set to "None", "Trace", "Debug", "Information", "Warning", "Error", or "Critical".

- __DISABLE_COLORS__ [STRING, DEFAULT: false]: If true, colors will be disabled in the logs. This is helpful for many logging systems other than the console.

- __APPCONFIG_URL__ [STRING, OPTIONAL]: The URL for the App Configuration service. This is used to pull settings from Azure App Configuration.

- __CONFIG_KEYS__ [STRING, OPTIONAL]: This is a comma-delimited list of configuration keys to pull for the specific service. All keys matching the pattern will be pulled. A setting that is already set is not replaced (so left-most patterns take precident). For example, the dev environment of the auth service might contain "app:auth:dev:*, app:common:dev:*". If you do not specify any CONFIG_KEYS, no variables will be set from App Configuration.

- __ASPNETCORE_ENVIRONMENT__ [STRING, DEFAULT: "Development"]: This is a common .NET setting. It is used by INCLUDE_CREDENTIAL_TYPES.

- __INCLUDE_CREDENTIAL_TYPES__ [STRING, *]: This is a comma-delimited list of credential types to consider when connecting to App Configuration, Key Vault, or using DefaultAzureCredential. It can include "env", "mi", "token", "vs", "vscode", "azcli", and/or "browser". If __ASPNETCORE_ENVIRONMENT__ is "Development", then the default is "azcli, env"; otherwise, the default is "env, mi". You can find out more about the options [here](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet).

## Configuring Search Modes

The search service is used to find related documents to a search query. Consider the following when configuring `SEARCH_MODE`:

| __SEARCH_MODE__ | Text Embeddings | Description | Performance Notes | __SEARCH_SEMANTIC_RERANK_CONFIG__ | Concurrency Notes | Doc Schema |
|-----------------|------------------|-------------|-------------------|-----------------------------|---------------|-----------------|
| `Keyword` | N/A | Only the raw search query is used to find related documents. Expect unbounded search scores. (ex. `"@search.score": 76.57179` or `"@search.score": 41.846764`). | N/A | N/A | Purely depends on the partition and replicas provisioned. | `{ "@search.rerankerScore": 0, "@search.score": 76.57179, "title": "", "content": "", "urls": [ ] },` |
| `Vector` | Required | Azure AI search will use a __single__ vector field from the index to perform a similarity search on the vectorized search query. Current implementation is configured to use HNSW filter with k at query time but exhaustive search is an option as well. Expect search scores within a specific range of `0.333 - 1.00`. (ex. `"@search.score": 0.71832`). |  When exhausitive search is enabled and the dimensionality of the vector embedding is high, search may perform slowly in order to return exact results. In this scenarios, HNSW (approx. knn) is preferred.  | N/A | Purely depends on the partition and replicas provisioned. |  `{ "@search.rerankerScore": 0, "@search.score": 0.71832, "title": "", "content": "", "urls": [ ] },` |
| `Hybrid` | Required | Azure AI search will use __both__ raw text and vectorized search query to independently find documents and then merge them using the RRF algorithm. Upper limit of score is bounded by the number of queries being fused. If 4 queries are being fused, score could be as high as a 4. (ex. `"@search.score": 4.000`).  | Hybrid queries aren't conducive to minimum thresholds because the RRF ranges are so much smaller and volatile.  It's recommended to set `SEARCH_KNN` to 50 in this mode. Also, `SEARCH_TOP` does not control how many results are considered in the raw text side of the query.  | N/A | Azure AI Search executes Hybrid queries in parallel. One for keyword and one for each vector field. | `{ "@search.rerankerScore": 0, "@search.score":  0.01815, "title": "", "content": "", "urls": [ ] },` |
| `KeywordWithSemanticRerank` | NA | The raw search query with semantic configuration is used to find related documents. Expect rerankerScores between 0 and 4. ( ex.`"@search.rerankerScore": 2.2135608196258545,`). <u> If Azure AI Search does not find a matching remote config with the same name, this mode will throw an 400 `Azure.RequestFailedException` and fail all requests if setting is not true.</u> |  It's recommended to set `SEARCH_TOP` (i.e. `Top`) to a high number in this mode. Currently, default is 10. | `default` | For semantic re-ranking, Azure AI Search supports up to 10 concurrent queries per replica. So 10 concurrent `KeywordWithSemanticRerank` queries per replica is an optimal number. | `{ "@search.rerankerScore": 2.2135608196258545, "@search.score":  41.43339, "title": "", "content": "", "urls": [ ] },` |
| `HybridWithSemanticRerank` | Required | Similar to `KeywordWithSemanticRerank`, except this also includes `vector` search as well. Expect rerankerScores between 0 and 4. | It's recommended to set `SEARCH_KNN` to 50 in this mode. Also, `SEARCH_TOP` does not control how many results are considered in the raw text side of the query. | `default` | Azure AI Search executes Hybrid queries in parallel. For semantic re-ranking, Azure AI Search supports up to 10 concurrent queries per replica. So 5 concurrent `HybridWithSemanticRerank` queries per replica might be an optimal number. | `{ "@search.rerankerScore": 2.324333667755127, "@search.score":  0.012048192322254181, "title": "", "content": "", "urls": [ ] },` |

Helpful links for understanding Azure AI Search:

- [HNSW Vector Search Runtime Behavior](https://learn.microsoft.com/en-us/azure/search/vector-search-ranking#navigating-the-hnsw-graph-at-query-time)
- [RFF Hybrid Search](https://learn.microsoft.com/en-us/azure/search/hybrid-search-ranking)
- [Azure Search Version 11.6.0](https://github.com/Azure/azure-sdk-for-net/blob/Azure.Search.Documents_11.6.0/sdk/search/Azure.Search.Documents/CHANGELOG.md)

## Running locally

To run the solution locally, you need to do the following:

```bash
dotnet run
```

## Endpoints

There is a swagger interface to view all endpoints and their model definitions at `http://localhost:7030/swagger`.

There are endpoints for each workflow:

- `http://localhost:7030/api/workflows/primary`: This workflow is used for most cases and includes the following steps in order: DetermineIntent, ApplyIntent, GetDocuments, SortDocuments, SelectGroundingData, GenerateAnswer.

- `http://localhost:7030/api/workflows/in-domain-only`: This workflow is similar to primary except the intent is always IN_DOMAIN. It includes the following steps in order: InDomainOnlyIntent, ApplyIntent, GetDocuments, SortDocuments, SelectGroundingData, GenerateAnswer.

- `http://localhost:7030/api/workflows/pick-docs`: This workflow is similar to primary except context is always pulled from the index based on what is in the ground truth (ie. perfect retrieval). It includes the following steps in order: DetermineIntent, ApplyIntent, PickDocuments, SortDocuments, SelectGroundingData, GenerateAnswer.

There are also endpoints for each workflow step.

## Building a container

You can build a container by going up a level in the folder structure and running the following:

```bash
docker build -t repo.azurecr.io/inference:2.5.0 -f inference.Dockerfile --platform linux/amd64 .
```

## TODO

- Add configuration details to the /workflow endpoint.
- Implement retry when DetermineIntent fails to deserialize.
- Convert to Fabrikam bike shop
  - including in-memory search capability - abstract search to interface.
  - including in-memory fake LLM responses.
- Verify that the swagger definition is correct.
- Incorporate custom instructions into the GenerateAnswer step.
- Unit testing/integration testing.
