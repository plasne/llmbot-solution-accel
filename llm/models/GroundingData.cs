using System.Collections.Generic;
using DistributedChat;

public class GroundingData
{
    public List<Doc>? Docs { get; set; }
    public List<string>? Content { get; set; }
    public string? UserQuery { get; set; }
    public List<Turn>? History { get; set; }
}