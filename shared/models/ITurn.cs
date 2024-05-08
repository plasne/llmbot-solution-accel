namespace Shared.Models.Memory;

public interface ITurn
{
    public Roles Role { get; set; }
    public string Msg { get; set; }
}