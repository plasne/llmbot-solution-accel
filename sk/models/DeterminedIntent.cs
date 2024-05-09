using System.Collections.Generic;
using Shared.Models.Memory;

public class DeterminedIntent(string query) : IDeterminedIntent
{
    public Intents Intent { get; set; }
    public string Query { get; set; } = query;
    public List<string>? SearchQueries { get; set; }
    public string? GameName { get; set; }
    public string? Edition { get; set; }
}