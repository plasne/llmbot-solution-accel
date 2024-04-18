using System.Diagnostics;

namespace llm;

public static class DiagnosticService
{
    const string SourceName = "Inference";
    public static readonly ActivitySource Source = new(SourceName);
}
