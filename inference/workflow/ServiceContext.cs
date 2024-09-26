using System.Collections.Generic;

namespace Inference;

public class ServiceContext : IServiceContext
{
    public ServiceContext(IConfig config)
    {
        this.combinations = new();
        if (config.SEARCH_MODE is SearchMode.Vector
            or SearchMode.Hybrid
            or SearchMode.HybridWithSemanticRerank)
        {
            foreach (var embed in config.EMBEDDING_CONNECTION_STRINGS)
            {
                foreach (var llm in config.LLM_CONNECTION_STRINGS)
                {
                    this.combinations.Add(new KernelIndex
                    {
                        EmbeddingConnectionDetails = embed,
                        LlmConnectionDetails = llm
                    });
                }
            }
        }
        else
        {
            foreach (var llm in config.LLM_CONNECTION_STRINGS)
            {
                this.combinations.Add(new KernelIndex
                {
                    LlmConnectionDetails = llm
                });
            }
        }
    }

    private readonly object padlock = new();
    private List<KernelIndex> combinations { get; set; }
    private int index = -1;

    public KernelIndex KernelIndex
    {
        get
        {
            lock (padlock)
            {
                index++;
                if (index >= this.combinations.Count)
                {
                    index = 0;
                }
                var combo = combinations[index];
                return new KernelIndex
                {
                    Index = index,
                    EmbeddingConnectionDetails = combo.EmbeddingConnectionDetails,
                    LlmConnectionDetails = combo.LlmConnectionDetails
                };
            }
        }
    }
}