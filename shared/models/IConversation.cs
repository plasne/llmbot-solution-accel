namespace Shared.Models.Memory;

public interface IConversation
{
    public Guid Id { get; set; }

    public IEnumerable<ITurn>? Turns { get; set; }

    public string? CustomInstructions { get; set; }
}