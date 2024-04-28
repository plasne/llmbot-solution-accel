using System;

public class AlreadyGeneratingException : Exception
{
    public AlreadyGeneratingException(string userId)
        : base($"user {userId} has already asked the assistant to generating a response.")
    {
    }
}