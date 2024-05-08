using System;
using System.Collections.Generic;
using Shared.Models.Memory;

public class Conversation : IConversation
{
    public Guid Id { get; set; }
    public IEnumerable<ITurn>? Turns { get; set; }
    public string? CustomInstructions { get; set; }
}