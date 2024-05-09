namespace Shared.Models.Memory;

public interface IConversation
{
    public Guid Id { get; set; }

    public IList<ITurn>? Turns { get; set; }

    public string? CustomInstructions { get; set; }
}