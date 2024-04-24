using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

public class DiagnosticService
{
    const string SourceName = "sk";
    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);
    static readonly Histogram<int> PromptTokenCount = Metrics.CreateHistogram<int>(name: "prompt_token_count", description: "Count of prompt tokens");
    static readonly Histogram<int> CompletionTokenCount = Metrics.CreateHistogram<int>(name: "completion_token_count", description: "Count of completion tokens");

    static readonly Histogram<double> CompletionTokensPerSec = Metrics.CreateHistogram<double>(name: "completion_tokens_per_second", "sec", description: "Completion tokens per second");

    public static void RecordPromptTokenCount(int tokenCount, string step)
    {
        var dic = new KeyValuePair<string, object?>("step", step);
        PromptTokenCount.Record(tokenCount, dic);
    }

    public static void RecordCompletionTokenCount(int tokenCount, string step)
    {
        var dic = new KeyValuePair<string, object?>("step", step);
        CompletionTokenCount.Record(tokenCount, dic);
    }

    public static void RecordTokensPerSecond(double tokensPerSecond, string step)
    {
        var dic = new KeyValuePair<string, object?>("step", step);
        CompletionTokensPerSec.Record(tokensPerSecond, dic);
    }
}
