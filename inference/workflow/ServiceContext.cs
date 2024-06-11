using System;

namespace Inference;

public class ServiceContext(IConfig config) : IServiceContext
{
    private readonly object lockEndpointIndex = new();
    private readonly int numOfLLMEndpoints = config.LLM_CONNECTION_STRINGS.Count;
    private int llmEndpointIndex = -1;
    public int GetLLMEndpointIndex()
    {
        lock (lockEndpointIndex)
        {
            llmEndpointIndex++;
            if (llmEndpointIndex >= numOfLLMEndpoints)
            {
                llmEndpointIndex = 0;
            }
            return llmEndpointIndex;
        }
    }
}
