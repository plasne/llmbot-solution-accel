using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bot;

public static class DiagnosticService
{
    const string SourceName = "bot";
    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);
    static readonly Histogram<int> TimeToFirstResponse = Metrics.CreateHistogram<int>("time_to_first_response", "ms", "Time to first response");
    static readonly Histogram<int> TimeToLastResponse = Metrics.CreateHistogram<int>("time_to_last_response", "ms", "Time to last response");

    // This histogram is used to record inter-token latency for llm streaming in terms of Time Per Output Token (TPOT).
    static readonly Histogram<int> TimePerOutputToken = Metrics.CreateHistogram<int>(name: "time_per_output_token", "ms", description: "Miliseconds per output token");

    public static void RecordTimeToFirstResponse(int time)
    {
        TimeToFirstResponse.Record(time);
    }

    public static void RecordTimeToLastResponse(int time)
    {
        TimeToLastResponse.Record(time);
    }
    
    public static void RecordTimePerOutputToken(int timePerOutputToken)
    {
        TimePerOutputToken.Record(timePerOutputToken);
    }
}
