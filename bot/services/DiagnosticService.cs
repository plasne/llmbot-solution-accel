using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

public class DiagnosticService
{
    const string SourceName = "bot";
    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);
    static readonly Histogram<long> TimeToFirstResponse = Metrics.CreateHistogram<long>("time_to_first_response", "ms", "Time to first response");
    static readonly Histogram<long> TimeToLastResponse = Metrics.CreateHistogram<long>("time_to_last_response", "ms", "Time to last response");

    public static void RecordTimeToFirstResponse(long time, int wordCount)
    {
        var dic = new KeyValuePair<string, object?>("word_count", wordCount);
        TimeToFirstResponse.Record(time, dic);
    }


    public static void RecordTimeToLastResponse(long time, int wordCount)
    {
        var dic = new KeyValuePair<string, object?>("word_count", wordCount);
        TimeToLastResponse.Record(time, dic);
    }
}
