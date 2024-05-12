using System.Diagnostics;

public class DiagnosticService
{
    const string SourceName = "memory";
    public static readonly ActivitySource Source = new(SourceName);
}
