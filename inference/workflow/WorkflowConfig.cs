using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inference;

public class WorkflowConfig(IConfig sysConfig, WorkflowRequestParameters? parameters) : IConfig
{
    private readonly IConfig sysConfig = sysConfig;
    private readonly WorkflowRequestParameters? parameters = parameters;

    [JsonProperty(nameof(GRPC_PORT), NullValueHandling = NullValueHandling.Ignore)]
    public int GRPC_PORT => this.sysConfig.GRPC_PORT;

    [JsonProperty(nameof(WEB_PORT), NullValueHandling = NullValueHandling.Ignore)]
    public int WEB_PORT => this.sysConfig.WEB_PORT;

    [JsonProperty(nameof(MEMORY_TERM), NullValueHandling = NullValueHandling.Ignore)]
    public MemoryTerm MEMORY_TERM => this.sysConfig.MEMORY_TERM;

    [JsonProperty(nameof(OPEN_TELEMETRY_CONNECTION_STRING), NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(JsonMaskConverter))]
    public string OPEN_TELEMETRY_CONNECTION_STRING => this.sysConfig.OPEN_TELEMETRY_CONNECTION_STRING;

    [JsonProperty(nameof(LLM_CONNECTION_STRINGS), NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(JsonMaskConverter))]
    public List<ModelConnectionDetails> LLM_CONNECTION_STRINGS => this.sysConfig.LLM_CONNECTION_STRINGS;

    [JsonProperty(nameof(LLM_MODEL_NAME), NullValueHandling = NullValueHandling.Ignore)]
    public string LLM_MODEL_NAME => this.sysConfig.LLM_MODEL_NAME;

    [JsonProperty(nameof(LLM_ENCODING_MODEL), NullValueHandling = NullValueHandling.Ignore)]
    public string LLM_ENCODING_MODEL { get => this.sysConfig.LLM_ENCODING_MODEL; set => throw new System.NotImplementedException(); }

    [JsonProperty(nameof(EMBEDDING_CONNECTION_STRINGS), NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(JsonMaskConverter))]
    public List<ModelConnectionDetails> EMBEDDING_CONNECTION_STRINGS => this.sysConfig.EMBEDDING_CONNECTION_STRINGS;

    [JsonProperty(nameof(EMBEDDING_MODEL_NAME), NullValueHandling = NullValueHandling.Ignore)]
    public string EMBEDDING_MODEL_NAME => this.sysConfig.EMBEDDING_MODEL_NAME;

    [JsonProperty(nameof(EMBEDDING_ENCODING_MODEL), NullValueHandling = NullValueHandling.Ignore)]
    public string EMBEDDING_ENCODING_MODEL { get => this.sysConfig.EMBEDDING_ENCODING_MODEL; set => throw new System.NotImplementedException(); }

    [JsonProperty(nameof(SEARCH_INDEX), NullValueHandling = NullValueHandling.Ignore)]
    public string SEARCH_INDEX => this.sysConfig.SEARCH_INDEX;

    [JsonProperty(nameof(SEARCH_ENDPOINT_URI), NullValueHandling = NullValueHandling.Ignore)]
    public string SEARCH_ENDPOINT_URI => this.sysConfig.SEARCH_ENDPOINT_URI;

    [JsonProperty(nameof(SEARCH_API_KEY), NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(JsonMaskConverter))]
    public string SEARCH_API_KEY => this.sysConfig.SEARCH_API_KEY;

    [JsonProperty(nameof(SEARCH_SEMANTIC_RERANK_CONFIG), NullValueHandling = NullValueHandling.Ignore)]
    public string SEARCH_SEMANTIC_RERANK_CONFIG => this.sysConfig.SEARCH_SEMANTIC_RERANK_CONFIG;

    [JsonProperty(nameof(SEARCH_MODE), NullValueHandling = NullValueHandling.Ignore)]
    public SearchMode SEARCH_MODE => this.sysConfig.SEARCH_MODE;

    [JsonProperty(nameof(SEARCH_VECTOR_FIELDS), NullValueHandling = NullValueHandling.Ignore)]
    public string[] SEARCH_VECTOR_FIELDS => this.sysConfig.SEARCH_VECTOR_FIELDS;

    [JsonProperty(nameof(SEARCH_SELECT_FIELDS), NullValueHandling = NullValueHandling.Ignore)]
    public string[] SEARCH_SELECT_FIELDS => this.sysConfig.SEARCH_SELECT_FIELDS;

    [JsonProperty(nameof(SEARCH_TRANSFORM_FILE), NullValueHandling = NullValueHandling.Ignore)]
    public string SEARCH_TRANSFORM_FILE => this.sysConfig.SEARCH_TRANSFORM_FILE;

    [JsonProperty(nameof(PICK_DOCS_URL_FIELD), NullValueHandling = NullValueHandling.Ignore)]
    public string PICK_DOCS_URL_FIELD => this.sysConfig.PICK_DOCS_URL_FIELD;

    [JsonProperty(nameof(MAX_CONCURRENT_SEARCHES), NullValueHandling = NullValueHandling.Ignore)]
    public int MAX_CONCURRENT_SEARCHES => this.parameters?.MAX_CONCURRENT_SEARCHES ?? this.sysConfig.MAX_CONCURRENT_SEARCHES;

    [JsonProperty(nameof(MAX_SEARCH_QUERIES_PER_INTENT), NullValueHandling = NullValueHandling.Ignore)]
    public int MAX_SEARCH_QUERIES_PER_INTENT => this.parameters?.MAX_SEARCH_QUERIES_PER_INTENT ?? this.sysConfig.MAX_SEARCH_QUERIES_PER_INTENT;

    [JsonProperty(nameof(MIN_RELEVANCE_SEARCH_SCORE), NullValueHandling = NullValueHandling.Ignore)]
    public decimal MIN_RELEVANCE_SEARCH_SCORE => this.sysConfig.MIN_RELEVANCE_SEARCH_SCORE;

    [JsonProperty(nameof(MIN_RELEVANCE_RERANK_SCORE), NullValueHandling = NullValueHandling.Ignore)]
    public decimal MIN_RELEVANCE_RERANK_SCORE => this.sysConfig.MIN_RELEVANCE_RERANK_SCORE;

    [JsonProperty(nameof(SEARCH_TOP), NullValueHandling = NullValueHandling.Ignore)]
    public int SEARCH_TOP => this.sysConfig.SEARCH_TOP;

    [JsonProperty(nameof(SEARCH_KNN), NullValueHandling = NullValueHandling.Ignore)]
    public int SEARCH_KNN => this.sysConfig.SEARCH_KNN;

    [JsonProperty(nameof(SEARCH_VECTOR_EXHAUST_KNN), NullValueHandling = NullValueHandling.Ignore)]
    public bool SEARCH_VECTOR_EXHAUST_KNN => this.sysConfig.SEARCH_VECTOR_EXHAUST_KNN;

    [JsonProperty(nameof(INTENT_PROMPT_FILE), NullValueHandling = NullValueHandling.Ignore)]
    public string INTENT_PROMPT_FILE => this.parameters?.INTENT_PROMPT_FILE ?? this.sysConfig.INTENT_PROMPT_FILE;

    [JsonProperty(nameof(CHAT_PROMPT_FILE), NullValueHandling = NullValueHandling.Ignore)]
    public string CHAT_PROMPT_FILE => this.parameters?.CHAT_PROMPT_FILE ?? this.sysConfig.CHAT_PROMPT_FILE;

    [JsonProperty(nameof(INTENT_TEMPERATURE), NullValueHandling = NullValueHandling.Ignore)]
    public decimal INTENT_TEMPERATURE => this.parameters?.INTENT_TEMPERATURE ?? this.sysConfig.INTENT_TEMPERATURE;

    [JsonProperty(nameof(CHAT_TEMPERATURE), NullValueHandling = NullValueHandling.Ignore)]
    public decimal CHAT_TEMPERATURE => this.parameters?.CHAT_TEMPERATURE ?? this.sysConfig.CHAT_TEMPERATURE;

    [JsonProperty(nameof(INTENT_SEED), NullValueHandling = NullValueHandling.Ignore)]
    public long? INTENT_SEED => this.sysConfig.INTENT_SEED;

    [JsonProperty(nameof(CHAT_SEED), NullValueHandling = NullValueHandling.Ignore)]
    public long? CHAT_SEED => this.sysConfig.CHAT_SEED;

    [JsonProperty(nameof(MEMORY_URL), NullValueHandling = NullValueHandling.Ignore)]
    public string MEMORY_URL => this.sysConfig.MEMORY_URL;

    [JsonProperty(nameof(MAX_RETRY_ATTEMPTS), NullValueHandling = NullValueHandling.Ignore)]
    public int MAX_RETRY_ATTEMPTS => this.sysConfig.MAX_RETRY_ATTEMPTS;

    [JsonProperty(nameof(SECONDS_BETWEEN_RETRIES), NullValueHandling = NullValueHandling.Ignore)]
    public int SECONDS_BETWEEN_RETRIES => this.sysConfig.SECONDS_BETWEEN_RETRIES;

    [JsonProperty(nameof(MAX_TIMEOUT_IN_SECONDS), NullValueHandling = NullValueHandling.Ignore)]
    public int MAX_TIMEOUT_IN_SECONDS => this.sysConfig.MAX_TIMEOUT_IN_SECONDS;

    [JsonProperty(nameof(EMIT_USAGE_AS_RESPONSE_HEADERS), NullValueHandling = NullValueHandling.Ignore)]
    public bool EMIT_USAGE_AS_RESPONSE_HEADERS => this.sysConfig.EMIT_USAGE_AS_RESPONSE_HEADERS;

    [JsonProperty(nameof(COST_PER_PROMPT_TOKEN), NullValueHandling = NullValueHandling.Ignore)]
    public decimal COST_PER_PROMPT_TOKEN => this.sysConfig.COST_PER_PROMPT_TOKEN;

    [JsonProperty(nameof(COST_PER_COMPLETION_TOKEN), NullValueHandling = NullValueHandling.Ignore)]
    public decimal COST_PER_COMPLETION_TOKEN => this.sysConfig.COST_PER_COMPLETION_TOKEN;

    [JsonProperty(nameof(COST_PER_EMBEDDING_TOKEN), NullValueHandling = NullValueHandling.Ignore)]
    public decimal COST_PER_EMBEDDING_TOKEN => this.sysConfig.COST_PER_EMBEDDING_TOKEN;

    [JsonProperty(nameof(SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT), NullValueHandling = NullValueHandling.Ignore)]
    public int SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT => this.sysConfig.SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT;

    [JsonProperty(nameof(EXIT_WHEN_OUT_OF_DOMAIN), NullValueHandling = NullValueHandling.Ignore)]
    public bool EXIT_WHEN_OUT_OF_DOMAIN => this.parameters?.EXIT_WHEN_OUT_OF_DOMAIN ?? this.sysConfig.EXIT_WHEN_OUT_OF_DOMAIN;

    [JsonProperty(nameof(EXIT_WHEN_NO_DOCUMENTS), NullValueHandling = NullValueHandling.Ignore)]
    public bool EXIT_WHEN_NO_DOCUMENTS => this.parameters?.EXIT_WHEN_NO_DOCUMENTS ?? this.sysConfig.EXIT_WHEN_NO_DOCUMENTS;

    [JsonProperty(nameof(EXIT_WHEN_NO_CITATIONS), NullValueHandling = NullValueHandling.Ignore)]
    public bool EXIT_WHEN_NO_CITATIONS => this.parameters?.EXIT_WHEN_NO_CITATIONS ?? this.sysConfig.EXIT_WHEN_NO_CITATIONS;

    public void Validate() => throw new System.NotImplementedException();
}
