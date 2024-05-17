using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Inference;

public static class DiagnosticService
{
    const string SourceName = "sk";
    const string Model = "model";
    const string Step = "step";

    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);

    static readonly Histogram<int> EmbeddingTokenCount = Metrics.CreateHistogram<int>(name: "embedding_token_count", description: "Count of embedding tokens");

    static readonly Histogram<int> PromptTokenCount = Metrics.CreateHistogram<int>(name: "prompt_token_count", description: "Count of prompt tokens");

    static readonly Histogram<int> CompletionTokenCount = Metrics.CreateHistogram<int>(name: "completion_token_count", description: "Count of completion tokens");

    static readonly Histogram<double> CompletionTokensPerSec = Metrics.CreateHistogram<double>(name: "completion_tokens_per_second", "sec", description: "Completion tokens per second");

    static readonly Histogram<int> SearchQueryCount = Metrics.CreateHistogram<int>("search_query_count", "Count of search queries executed");

    public static void RecordSearchQueryCount(int count)
    {
        SearchQueryCount.Record(count);
    }

    public static void RecordEmbeddingTokenCount(int tokenCount, string modelName, string step)
    {
        var modelTag = new KeyValuePair<string, object?>(Model, modelName);
        var stepTag = new KeyValuePair<string, object?>(Step, step);
        EmbeddingTokenCount.Record(tokenCount, modelTag, stepTag);
    }

    public static void RecordPromptTokenCount(int tokenCount, string modelName, string step)
    {
        var modelTag = new KeyValuePair<string, object?>(Model, modelName);
        var stepTag = new KeyValuePair<string, object?>(Step, step);
        PromptTokenCount.Record(tokenCount, modelTag, stepTag);
    }

    public static void RecordCompletionTokenCount(int tokenCount, string modelName, string step)
    {
        var modelTag = new KeyValuePair<string, object?>(Model, modelName);
        var stepTag = new KeyValuePair<string, object?>(Step, step);
        CompletionTokenCount.Record(tokenCount, modelTag, stepTag);
    }

    public static void RecordTokensPerSecond(double tokensPerSecond, string modelName, string step)
    {
        var modelTag = new KeyValuePair<string, object?>(Model, modelName);
        var stepTag = new KeyValuePair<string, object?>(Step, step);
        CompletionTokensPerSec.Record(tokensPerSecond, modelTag, stepTag);
    }

}
