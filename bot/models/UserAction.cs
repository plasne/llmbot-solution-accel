using Shared.Models.Memory;

namespace Bot;

public class UserAction
{
    public string? ActivityId;
    public string? Comment;
    public string? Command;
    public string? Reply;
    public Citation[]? Citations;
}