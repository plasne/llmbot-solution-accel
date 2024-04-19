using System.Diagnostics;
using System.Diagnostics.Metrics;

public class DiagnosticService
{
    const string SourceName = "bot";
    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);
    public static readonly Histogram<long> TimeToFirstResponse = Metrics.CreateHistogram<long>("time_to_first_response", "ms", "Time to first response");
    public static readonly Histogram<long> TimeToLastResponse = Metrics.CreateHistogram<long>("time_to_last_response", "ms", "Time to last response");
}
