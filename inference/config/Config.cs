using System.Collections.Generic;
using NetBricks;
using SharpToken;

namespace Inference;

public class Config : IConfig
{
    private readonly NetBricks.IConfig config;

    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.GRPC_PORT = config.Get<string>("GRPC_PORT").AsInt(() => 7020);
        this.WEB_PORT = config.Get<string>("WEB_PORT").AsInt(() => 7030);
        this.OPEN_TELEMETRY_CONNECTION_STRING = config.GetSecret<string>("OPEN_TELEMETRY_CONNECTION_STRING").Result;
        this.MEMORY_TERM = config.Get<string>("MEMORY_TERM").AsEnum(() => MemoryTerm.Long);
        this.LLM_CONNECTION_STRINGS = config.GetSecret<string>("LLM_CONNECTION_STRINGS").Result.AsModelConnectionDetails(() => []);
        this.LLM_MODEL_NAME = config.Get<string>("LLM_MODEL_NAME");
        this.LLM_ENCODING_MODEL = "TBD";
        this.EMBEDDING_CONNECTION_STRINGS = config.GetSecret<string>("LLM_CONNECTION_STRINGS").Result.AsModelConnectionDetails(() => []);
        this.EMBEDDING_MODEL_NAME = config.Get<string>("EMBEDDING_MODEL_NAME");
        this.EMBEDDING_ENCODING_MODEL = "TBD";
        this.SEARCH_INDEX = config.Get<string>("SEARCH_INDEX");
        this.SEARCH_ENDPOINT_URI = config.Get<string>("SEARCH_ENDPOINT_URI");
        this.SEARCH_API_KEY = config.GetSecret<string>("SEARCH_API_KEY").Result;
        this.SEARCH_MODE = config.Get<string>("SEARCH_MODE").AsSearchMode(() => SearchMode.HybridWithSemanticRerank);
        this.SEARCH_SEMANTIC_RERANK_CONFIG = config.Get<string>("SEARCH_SEMANTIC_RERANK_CONFIG").AsString(() => "default");
        this.SEARCH_VECTOR_FIELDS = config.Get<string>("SEARCH_VECTOR_FIELDS").AsArray(() => ["contentVector"]);
        this.SEARCH_SELECT_FIELDS = config.Get<string>("SEARCH_SELECT_FIELDS").AsArray(() => ["title", "content", "urls"]);
        this.SEARCH_TRANSFORM_FILE = this.config.Get<string>("SEARCH_TRANSFORM_FILE");
        this.PICK_DOCS_URL_FIELD = config.Get<string>("PICK_DOCS_URL_FIELD").AsString(() => "url");
        this.MAX_CONCURRENT_SEARCHES = config.Get<string>("MAX_CONCURRENT_SEARCHES").AsInt(() => 3);
        this.MAX_SEARCH_QUERIES_PER_INTENT = config.Get<string>("MAX_SEARCH_QUERIES_PER_INTENT").AsInt(() => 3);
        this.MIN_RELEVANCE_SEARCH_SCORE = config.Get<string>("MIN_RELEVANCE_SEARCH_SCORE").AsDecimal(() => 0.0m);
        this.MIN_RELEVANCE_RERANK_SCORE = config.Get<string>("MIN_RELEVANCE_RERANK_SCORE").AsDecimal(() => 2.0m);
        this.SEARCH_TOP = config.Get<string>("SEARCH_TOP").AsInt(() => 10);
        this.SEARCH_KNN = config.Get<string>("SEARCH_KNN").AsInt(() => 10);
        this.SEARCH_VECTOR_EXHAUST_KNN = config.Get<string>("SEARCH_VECTOR_EXHAUST_KNN").AsBool(() => false);
        this.INTENT_PROMPT_FILE = config.Get<string>("INTENT_PROMPT_FILE").AsString(() => "./templates/intent-prompt.txt");
        this.CHAT_PROMPT_FILE = config.Get<string>("CHAT_PROMPT_FILE").AsString(() => "./templates/chat-prompt.txt");
        this.INTENT_TEMPERATURE = config.Get<string>("INTENT_TEMPERATURE").AsDecimal(() => 0.0m);
        this.CHAT_TEMPERATURE = config.Get<string>("CHAT_TEMPERATURE").AsDecimal(() => 0.3m);
        this.INTENT_SEED = config.Get<string>("INTENT_SEED, SEED").AsOptionalLong(() => null);
        this.CHAT_SEED = config.Get<string>("CHAT_SEED, SEED").AsOptionalLong(() => null);
        this.MEMORY_URL = this.config.Get<string>("MEMORY_URL").AsString(() => "http://localhost:7010");
        this.MAX_RETRY_ATTEMPTS = config.Get<string>("MAX_RETRY_ATTEMPTS").AsInt(() => 3);
        this.SECONDS_BETWEEN_RETRIES = config.Get<string>("SECONDS_BETWEEN_RETRIES").AsInt(() => 2);
        this.MAX_TIMEOUT_IN_SECONDS = config.Get<string>("MAX_TIMEOUT_IN_SECONDS").AsInt(() => 60);
        this.EMIT_USAGE_AS_RESPONSE_HEADERS = config.Get<string>("EMIT_USAGE_AS_RESPONSE_HEADERS").AsBool(() => false);
        this.COST_PER_PROMPT_TOKEN = config.Get<string>("COST_PER_PROMPT_TOKEN").AsDecimal(() => 0.0m);
        this.COST_PER_COMPLETION_TOKEN = config.Get<string>("COST_PER_COMPLETION_TOKEN").AsDecimal(() => 0.0m);
        this.COST_PER_EMBEDDING_TOKEN = config.Get<string>("COST_PER_EMBEDDING_TOKEN").AsDecimal(() => 0.0m);
        this.SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT = config.Get<string>("SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT").AsInt(() => 14000);
        this.EXIT_WHEN_OUT_OF_DOMAIN = config.Get<string>("EXIT_WHEN_OUT_OF_DOMAIN").AsBool(() => true);
        this.EXIT_WHEN_NO_DOCUMENTS = config.Get<string>("EXIT_WHEN_NO_DOCUMENTS").AsBool(() => true);
        this.EXIT_WHEN_NO_CITATIONS = config.Get<string>("EXIT_WHEN_NO_CITATIONS").AsBool(() => false);
    }

    public int GRPC_PORT { get; }

    public int WEB_PORT { get; }

    public string OPEN_TELEMETRY_CONNECTION_STRING { get; }

    public MemoryTerm MEMORY_TERM { get; }

    public List<ModelConnectionDetails> LLM_CONNECTION_STRINGS { get; }

    public string LLM_MODEL_NAME { get; }

    public string LLM_ENCODING_MODEL { get; set; }

    public List<ModelConnectionDetails> EMBEDDING_CONNECTION_STRINGS { get; }

    public string EMBEDDING_MODEL_NAME { get; }

    public string EMBEDDING_ENCODING_MODEL { get; set; }

    public string SEARCH_INDEX { get; }

    public string SEARCH_ENDPOINT_URI { get; }

    public string SEARCH_API_KEY { get; }

    public string SEARCH_SEMANTIC_RERANK_CONFIG { get; }

    public SearchMode SEARCH_MODE { get; }

    public string[] SEARCH_VECTOR_FIELDS { get; }

    public string[] SEARCH_SELECT_FIELDS { get; }

    public string SEARCH_TRANSFORM_FILE { get; }

    public string PICK_DOCS_URL_FIELD { get; }

    public int MAX_CONCURRENT_SEARCHES { get; }

    public int MAX_SEARCH_QUERIES_PER_INTENT { get; }

    public decimal MIN_RELEVANCE_SEARCH_SCORE { get; }

    public decimal MIN_RELEVANCE_RERANK_SCORE { get; }

    public int SEARCH_TOP { get; }

    public int SEARCH_KNN { get; }

    public bool SEARCH_VECTOR_EXHAUST_KNN { get; }

    public string INTENT_PROMPT_FILE { get; }

    public string CHAT_PROMPT_FILE { get; }

    public decimal INTENT_TEMPERATURE { get; }

    public decimal CHAT_TEMPERATURE { get; }

    public long? INTENT_SEED { get; }

    public long? CHAT_SEED { get; }

    public string MEMORY_URL { get; }

    public int MAX_RETRY_ATTEMPTS { get; }

    public int SECONDS_BETWEEN_RETRIES { get; }

    public int MAX_TIMEOUT_IN_SECONDS { get; }

    public bool EMIT_USAGE_AS_RESPONSE_HEADERS { get; }

    public decimal COST_PER_PROMPT_TOKEN { get; }

    public decimal COST_PER_COMPLETION_TOKEN { get; }

    public decimal COST_PER_EMBEDDING_TOKEN { get; }

    public int SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT { get; }

    public bool EXIT_WHEN_OUT_OF_DOMAIN { get; }

    public bool EXIT_WHEN_NO_DOCUMENTS { get; }

    public bool EXIT_WHEN_NO_CITATIONS { get; }

    public void Validate()
    {
        this.config.Require("GRPC_PORT", this.GRPC_PORT);
        this.config.Require("WEB_PORT", this.WEB_PORT);
        this.config.Optional("OPEN_TELEMETRY_CONNECTION_STRING", this.OPEN_TELEMETRY_CONNECTION_STRING, hideValue: true);
        this.config.Require("MEMORY_TERM", this.MEMORY_TERM.ToString());

        this.config.Optional("LLM_CONNECTION_STRINGS", $"({this.LLM_CONNECTION_STRINGS.Count} set)");
        if (this.LLM_CONNECTION_STRINGS.Count > 0)
        {
            this.config.Require("LLM_MODEL_NAME", this.LLM_MODEL_NAME);
            this.LLM_ENCODING_MODEL = Model.GetEncodingNameForModel(this.LLM_MODEL_NAME);
            this.config.Require("LLM_ENCODING_MODEL", this.LLM_ENCODING_MODEL);
        }

        this.config.Optional("SEARCH_INDEX", this.SEARCH_INDEX);
        if (!string.IsNullOrEmpty(this.SEARCH_INDEX))
        {
            this.config.Require("SEARCH_ENDPOINT_URI", this.SEARCH_ENDPOINT_URI);
            this.config.Require("SEARCH_API_KEY", this.SEARCH_API_KEY, hideValue: true);
            this.config.Require("SEARCH_MODE", this.SEARCH_MODE.ToString());
            this.config.Require("PICK_DOCS_URL_FIELD", this.PICK_DOCS_URL_FIELD);
        }

        // vectorization settings
        if (this.LLM_CONNECTION_STRINGS.Count > 0 &&
            this.SEARCH_MODE is SearchMode.Vector or SearchMode.Hybrid or SearchMode.HybridWithSemanticRerank)
        {
            this.config.Require("EMBEDDING_CONNECTION_STRINGS", this.EMBEDDING_CONNECTION_STRINGS.Count > 0
                ? $"({this.EMBEDDING_CONNECTION_STRINGS.Count} set)"
                : string.Empty);
            this.config.Require("EMBEDDING_MODEL_NAME", this.EMBEDDING_MODEL_NAME);
            this.EMBEDDING_ENCODING_MODEL = Model.GetEncodingNameForModel(this.EMBEDDING_MODEL_NAME);
            this.config.Require("EMBEDDING_ENCODING_MODEL", this.EMBEDDING_ENCODING_MODEL);

            this.config.Require("SEARCH_VECTOR_FIELDS", this.SEARCH_VECTOR_FIELDS);
            this.config.Require("SEARCH_VECTOR_EXHAUST_KNN", value: this.SEARCH_VECTOR_EXHAUST_KNN);
            this.config.Require("SEARCH_KNN", this.SEARCH_KNN);
        }

        // rerank settings
        if (this.LLM_CONNECTION_STRINGS.Count > 0 &&
            this.SEARCH_MODE is SearchMode.KeywordWithSemanticRerank or SearchMode.HybridWithSemanticRerank)
        {
            this.config.Require("MIN_RELEVANCE_RERANK_SCORE", ((double)this.MIN_RELEVANCE_RERANK_SCORE).ToString());
            this.config.Require("SEARCH_SEMANTIC_RERANK_CONFIG", this.SEARCH_SEMANTIC_RERANK_CONFIG);
        }

        this.config.Require("SEARCH_SELECT_FIELDS", this.SEARCH_SELECT_FIELDS);
        this.config.Optional("SEARCH_TRANSFORM_FILE", this.SEARCH_TRANSFORM_FILE);
        this.config.Require("MAX_CONCURRENT_SEARCHES", this.MAX_CONCURRENT_SEARCHES > 0
            ? $"({this.MAX_CONCURRENT_SEARCHES} set)"
            : string.Empty);
        this.config.Require("MAX_SEARCH_QUERIES_PER_INTENT", this.MAX_SEARCH_QUERIES_PER_INTENT);
        this.config.Require("MIN_RELEVANCE_SEARCH_SCORE", ((double)this.MIN_RELEVANCE_SEARCH_SCORE).ToString());
        this.config.Require("SEARCH_TOP", this.SEARCH_TOP);
        this.config.Require("INTENT_PROMPT_FILE", this.INTENT_PROMPT_FILE);
        this.config.Require("CHAT_PROMPT_FILE", this.CHAT_PROMPT_FILE);
        this.config.Require("INTENT_TEMPERATURE", this.INTENT_TEMPERATURE.ToString());
        this.config.Require("CHAT_TEMPERATURE", this.CHAT_TEMPERATURE.ToString());
        this.config.Optional("INTENT_SEED", this.INTENT_SEED.ToString());
        this.config.Optional("CHAT_SEED", this.CHAT_SEED.ToString());
        this.config.Require("MEMORY_URL", this.MEMORY_URL);
        this.config.Require("MAX_RETRY_ATTEMPTS", this.MAX_RETRY_ATTEMPTS);
        this.config.Require("SECONDS_BETWEEN_RETRIES", this.SECONDS_BETWEEN_RETRIES);
        this.config.Require("MAX_TIMEOUT_IN_SECONDS", this.MAX_TIMEOUT_IN_SECONDS);
        this.config.Require("EMIT_USAGE_AS_RESPONSE_HEADERS", value: this.EMIT_USAGE_AS_RESPONSE_HEADERS);
        this.config.Require("COST_PER_PROMPT_TOKEN", this.COST_PER_PROMPT_TOKEN.ToString());
        this.config.Require("COST_PER_COMPLETION_TOKEN", this.COST_PER_COMPLETION_TOKEN.ToString());
        this.config.Require("COST_PER_EMBEDDING_TOKEN", this.COST_PER_EMBEDDING_TOKEN.ToString());
        this.config.Require("SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT", this.SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT);
        this.config.Optional("EXIT_WHEN_OUT_OF_DOMAIN", value: this.EXIT_WHEN_OUT_OF_DOMAIN);
        this.config.Optional("EXIT_WHEN_NO_DOCUMENTS", value: this.EXIT_WHEN_NO_DOCUMENTS);
        this.config.Optional("EXIT_WHEN_NO_CITATIONS", value: this.EXIT_WHEN_NO_CITATIONS);
    }
}