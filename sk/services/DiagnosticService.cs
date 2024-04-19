using System.Diagnostics;

namespace llm;

public class DiagnosticService
{
    const string SourceName = "sk";
    public static readonly ActivitySource Source = new(SourceName);
}
