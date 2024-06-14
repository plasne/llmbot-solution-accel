using System.Collections.Generic;
using System.IO;
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
        this.LLM_CONNECTION_STRINGS = config.GetSecret<string>("LLM_CONNECTION_STRINGS").Result.AsLlmConnectionDetails(() => []);
        this.MEMORY_TERM = config.Get<string>("MEMORY_TERM").AsEnum(() => MemoryTerm.Long);
        this.EMBEDDING_DEPLOYMENT_NAME = config.Get<string>("EMBEDDING_DEPLOYMENT_NAME");
        this.EMBEDDING_ENDPOINT_URI = config.Get<string>("EMBEDDING_ENDPOINT_URI");
        this.EMBEDDING_API_KEY = config.GetSecret<string>("EMBEDDING_API_KEY").Result;
        this.LLM_MODEL_NAME = config.Get<string>("LLM_MODEL_NAME");
        this.LLM_ENCODING_MODEL = "TBD";
        this.SEARCH_INDEX = config.Get<string>("SEARCH_INDEX");
        this.SEARCH_ENDPOINT_URI = config.Get<string>("SEARCH_ENDPOINT_URI");
        this.SEARCH_API_KEY = config.GetSecret<string>("SEARCH_API_KEY").Result;
        this.SEARCH_SEMANTIC_CONFIG = config.Get<string>("SEARCH_SEMANTIC_CONFIG").AsString(() => "default");
        this.SEARCH_VECTOR_FIELDS = config.Get<string>("SEARCH_VECTOR_FIELDS").AsArray(() => ["contentVector"]);
        this.SEARCH_SELECT_FIELDS = config.Get<string>("SEARCH_SELECT_FIELDS").AsArray(() => ["title", "content", "url"]);
        this.SEARCH_TRANSFORM_FILE = this.config.Get<string>("SEARCH_TRANSFORM_FILE");
        this.INTENT_PROMPT_FILE = config.Get<string>("INTENT_PROMPT_FILE").AsString(() => "./templates/intent-prompt.txt");
        this.CHAT_PROMPT_FILE = config.Get<string>("CHAT_PROMPT_FILE").AsString(() => "./templates/chat-prompt.txt");
        this.MEMORY_URL = this.config.Get<string>("MEMORY_URL").AsString(() => "http://localhost:7010");
        this.MAX_RETRY_ATTEMPTS = config.Get<string>("MAX_RETRY_ATTEMPTS").AsInt(() => 3);
        this.SECONDS_BETWEEN_RETRIES = config.Get<string>("SECONDS_BETWEEN_RETRIES").AsInt(() => 2);
        this.MAX_TIMEOUT_IN_SECONDS = config.Get<string>("MAX_TIMEOUT_IN_SECONDS").AsInt(() => 60);
        this.EMIT_USAGE_AS_RESPONSE_HEADERS = config.Get<string>("EMIT_USAGE_AS_RESPONSE_HEADERS").AsBool(() => false);
        this.COST_PER_PROMPT_TOKEN = config.Get<string>("COST_PER_PROMPT_TOKEN").AsDecimal(() => 0.0m);
        this.COST_PER_COMPLETION_TOKEN = config.Get<string>("COST_PER_COMPLETION_TOKEN").AsDecimal(() => 0.0m);
    }

    public int GRPC_PORT { get; }

    public int WEB_PORT { get; }

    public string OPEN_TELEMETRY_CONNECTION_STRING { get; }

    public List<LlmConnectionDetails> LLM_CONNECTION_STRINGS { get; }

    public MemoryTerm MEMORY_TERM { get; }

    public string EMBEDDING_DEPLOYMENT_NAME { get; }

    public string EMBEDDING_ENDPOINT_URI { get; }

    public string EMBEDDING_API_KEY { get; }

    public string LLM_MODEL_NAME { get; }

    public string LLM_ENCODING_MODEL { get; set; }

    public string SEARCH_INDEX { get; }

    public string SEARCH_ENDPOINT_URI { get; }

    public string SEARCH_API_KEY { get; }

    public string SEARCH_SEMANTIC_CONFIG { get; }

    public string[] SEARCH_VECTOR_FIELDS { get; }

    public string[] SEARCH_SELECT_FIELDS { get; }

    public string SEARCH_TRANSFORM_FILE { get; }

    public string INTENT_PROMPT_FILE { get; }

    public string CHAT_PROMPT_FILE { get; }

    public string MEMORY_URL { get; }

    public int MAX_RETRY_ATTEMPTS { get; }

    public int SECONDS_BETWEEN_RETRIES { get; }

    public int MAX_TIMEOUT_IN_SECONDS { get; }

    public bool EMIT_USAGE_AS_RESPONSE_HEADERS { get; }

    public decimal COST_PER_PROMPT_TOKEN { get; }

    public decimal COST_PER_COMPLETION_TOKEN { get; }

    public void Validate()
    {
        this.config.Require("GRPC_PORT", this.GRPC_PORT);
        this.config.Require("WEB_PORT", this.WEB_PORT);
        this.config.Require("OPEN_TELEMETRY_CONNECTION_STRING", this.OPEN_TELEMETRY_CONNECTION_STRING, hideValue: true);
        this.config.Require("LLM_CONNECTION_STRINGS", this.LLM_CONNECTION_STRINGS.Count > 0 ? "(set)" : string.Empty);
        this.config.Require("MEMORY_TERM", this.MEMORY_TERM.ToString());
        this.config.Require("EMBEDDING_DEPLOYMENT_NAME", this.EMBEDDING_DEPLOYMENT_NAME);
        this.config.Require("EMBEDDING_ENDPOINT_URI", this.EMBEDDING_ENDPOINT_URI);
        this.config.Require("EMBEDDING_API_KEY", this.EMBEDDING_API_KEY, hideValue: true);

        this.config.Require("LLM_MODEL_NAME", this.LLM_MODEL_NAME);
        this.LLM_ENCODING_MODEL = Model.GetEncodingNameForModel(this.LLM_MODEL_NAME);
        this.config.Require("LLM_ENCODING_MODEL", this.LLM_ENCODING_MODEL);

        this.config.Require("SEARCH_INDEX", this.SEARCH_INDEX);
        this.config.Require("SEARCH_ENDPOINT_URI", this.SEARCH_ENDPOINT_URI);
        this.config.Require("SEARCH_API_KEY", this.SEARCH_API_KEY, hideValue: true);
        this.config.Require("SEARCH_SEMANTIC_CONFIG", this.SEARCH_SEMANTIC_CONFIG);
        this.config.Require("SEARCH_VECTOR_FIELDS", this.SEARCH_VECTOR_FIELDS);
        this.config.Require("SEARCH_SELECT_FIELDS", this.SEARCH_SELECT_FIELDS);
        this.config.Optional("SEARCH_TRANSFORM_FILE", this.SEARCH_TRANSFORM_FILE);
        this.config.Require("INTENT_PROMPT_FILE", this.INTENT_PROMPT_FILE);
        this.config.Require("CHAT_PROMPT_FILE", this.CHAT_PROMPT_FILE);
        this.config.Require("MEMORY_URL", this.MEMORY_URL);
        this.config.Require("MAX_RETRY_ATTEMPTS", this.MAX_RETRY_ATTEMPTS);
        this.config.Require("SECONDS_BETWEEN_RETRIES", this.SECONDS_BETWEEN_RETRIES);
        this.config.Require("MAX_TIMEOUT_IN_SECONDS", this.MAX_TIMEOUT_IN_SECONDS);
        this.config.Require("EMIT_USAGE_AS_RESPONSE_HEADERS", value: this.EMIT_USAGE_AS_RESPONSE_HEADERS);
        this.config.Require("COST_PER_PROMPT_TOKEN", this.COST_PER_PROMPT_TOKEN.ToString());
        this.config.Require("COST_PER_COMPLETION_TOKEN", this.COST_PER_COMPLETION_TOKEN.ToString());
    }
}