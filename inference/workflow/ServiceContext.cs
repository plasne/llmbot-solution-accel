namespace Inference;

public class ServiceContext(IConfig config): IServiceContext
{
    private readonly object lockEndpointIndex = new object();

    private int numOfAIChatEndpoints = config.CHAT_LLM_CONNECTION_STRINGS.Length;

    private int AIChatEndpointIndex = 0;

    public int GetAIChatEndpointIndex()
    {
        lock (lockEndpointIndex)
        {
            var currentEndpointIndex = this.AIChatEndpointIndex;
            AdvanceEndpointIndex();
            return currentEndpointIndex;
        }
    }

    private void AdvanceEndpointIndex()
    {
        lock (lockEndpointIndex)
        {
            AIChatEndpointIndex++;
            if (AIChatEndpointIndex >= numOfAIChatEndpoints - 1)
            {
                AIChatEndpointIndex = 0;
            }
        }
    }
}
