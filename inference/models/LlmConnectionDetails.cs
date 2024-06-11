namespace Inference;

public class LlmConnectionDetails
{
    public required string DeploymentName { get; set; }
    public required string Endpoint { get; set; }
    public required string ApiKey { get; set; }
}