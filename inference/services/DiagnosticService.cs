using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Inference;

public static class DiagnosticService
{
    const string SourceName = "sk";
    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);
    static readonly Histogram<int> PromptTokenCount = Metrics.CreateHistogram<int>(name: "prompt_token_count", description: "Count of prompt tokens");
    static readonly Histogram<int> CompletionTokenCount = Metrics.CreateHistogram<int>(name: "completion_token_count", description: "Count of completion tokens");
    static readonly Histogram<double> CompletionTokensPerSec = Metrics.CreateHistogram<double>(name: "completion_tokens_per_second", "sec", description: "Completion tokens per second");

    private static TagList AddBaggage(TagList tags)
    {
        if (Activity.Current is null) return tags;
        foreach (var bag in Activity.Current.Baggage)
        {
            tags.Add(bag.Key, bag.Value);
        }
        return tags;
    }

    public static void RecordPromptTokenCount(int tokenCount, string modelName)
    {
        var tags = new TagList() { { "model", modelName } };
        tags = AddBaggage(tags);
        PromptTokenCount.Record(tokenCount, tags);
    }

    public static void RecordCompletionTokenCount(int tokenCount, string modelName)
    {
        var tags = new TagList() { { "model", modelName } };
        tags = AddBaggage(tags);
        CompletionTokenCount.Record(tokenCount, tags);
    }

    public static void RecordTokensPerSecond(double tokensPerSecond, string modelName)
    {
        var tags = new TagList() { { "model", modelName } };
        tags = AddBaggage(tags);
        CompletionTokensPerSec.Record(tokensPerSecond, tags);
    }
}
