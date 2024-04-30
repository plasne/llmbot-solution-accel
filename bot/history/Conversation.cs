using System;
using System.Collections.Generic;

public class Conversation
{
    public Guid? Id { get; set; }

    public List<Interaction>? Interactions { get; set; }

    public string? CustomInstructions { get; set; }
}