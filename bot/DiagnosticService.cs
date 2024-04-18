using System.Diagnostics;

namespace llm;

public class DiagnosticService
{
    const string SourceName = "Bot";
    public static readonly ActivitySource Source = new(SourceName);
}
