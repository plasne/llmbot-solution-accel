using System.Diagnostics;

public class DiagnosticService
{
    const string SourceName = "sk";
    public static readonly ActivitySource Source = new(SourceName);
}
