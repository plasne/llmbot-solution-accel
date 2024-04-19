using System.Diagnostics;

namespace llm;

public class DiagnosticService
{
    const string SourceName = "bot";
    public static readonly ActivitySource Source = new(SourceName);
}
