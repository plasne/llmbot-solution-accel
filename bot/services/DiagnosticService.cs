using System.Diagnostics;
using System.Diagnostics.Metrics;

public class DiagnosticService
{
    const string SourceName = "bot";
    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);
    static readonly Histogram<int> TimeToFirstResponse = Metrics.CreateHistogram<int>("time_to_first_response", "ms", "Time to first response");
    static readonly Histogram<int> TimeToLastResponse = Metrics.CreateHistogram<int>("time_to_last_response", "ms", "Time to last response");

    public static void RecordTimeToFirstResponse(int time)
    {
        TimeToFirstResponse.Record(time);
    }

    public static void RecordTimeToLastResponse(int time)
    {
        TimeToLastResponse.Record(time);
    }
}
