using System.Diagnostics;

namespace Memory;

public static class DiagnosticService
{
    const string SourceName = "memory";
    public static readonly ActivitySource Source = new(SourceName);
}
