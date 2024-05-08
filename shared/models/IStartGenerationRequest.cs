namespace Shared.Models.Memory;

public interface IStartGenerationRequest
{
    public string RequestActivityId { get; set; }

    public string Query { get; set; }

    public string ResponseActivityId { get; set; }
}