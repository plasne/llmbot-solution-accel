using System;

namespace Memory;

public class InteractionNotFoundException : Exception
{
    public InteractionNotFoundException(string userId)
        : base($"Interaction for user '{userId}' not found.")
    {
    }
}