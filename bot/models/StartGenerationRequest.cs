using Shared.Models.Memory;

public class StartGenerationRequest(
    string requestActivityId,
    string query,
    string responseActivityId)
    : IStartGenerationRequest
{
    public string RequestActivityId { get; set; } = requestActivityId;
    public string Query { get; set; } = query;
    public string ResponseActivityId { get; set; } = responseActivityId;
}