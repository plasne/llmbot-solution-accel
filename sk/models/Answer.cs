using System.Collections.Generic;
using DistributedChat;

public class Answer : IAnswer
{
    public string? Text { get; set; }
    public List<Citation>? Citations { get; set; }
}