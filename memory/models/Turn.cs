using Shared.Models.Memory;

public class Turn : ITurn
{
    public Roles Role { get; set; }
    public string Msg { get; set; } = "";
}