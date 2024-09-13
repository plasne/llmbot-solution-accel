using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpToken;

namespace Inference;

public class SelectGroundingData(IWorkflowContext context, ILogger<SelectGroundingData> logger)
    : BaseStep<GroundingData, GroundingData>(logger)
{
    private readonly IWorkflowContext context = context;
    public override string Name => "SelectGroundingData";

    public override Task<GroundingData> ExecuteInternal(
        GroundingData input,
        CancellationToken cancellationToken = default)
    {
        GroundingData output = new() { UserQuery = input.UserQuery };

        // Some ideas on what to do here:
        // - summarize history

        var encoding = GptEncoding.GetEncoding(context.Config.LLM_ENCODING_MODEL);
        int historyCount = input.History?.Count ?? 0;
        int contextCount = input.Docs?.Count ?? 0;

        int currentTokenCount = 0;
        output.Context = [];
        output.History = [];

        // determine the available context window size
        var lengthOfUserQuery = encoding.CountTokens(input.UserQuery);
        int contextWindowLimit = context.Config.SELECT_GROUNDING_CONTEXT_WINDOW_LIMIT - lengthOfUserQuery;
        if (contextWindowLimit < 1)
        {
            this.LogDebug("the user query consumed the entire context window, discarded {discard_context_count} context, {discard_history_count} history",
              contextCount,
              historyCount);
            return Task.FromResult(output);
        }

        // add from history and context
        while (historyCount > 0 || contextCount > 0)
        {

            // prefer including context over history by 2:1
            for (var i = 0; i < 2; i++)
            {
                if (contextCount > 0)
                {
                    var currentDoc = input.Docs?[output.Context.Count];
                    if (currentDoc is not null)
                    {
                        var context = GetContext(currentDoc, output.Context.Count);
                        int tokenCount = encoding.CountTokens(context.Text);
                        if (currentTokenCount + tokenCount > contextWindowLimit)
                        {

                            this.LogDebug("context window reached at context index {context_index}, discarded {discard_context_count} context, {discard_history_count} history, current context window: {current_context_window}",
                              output.Context.Count, input.Docs?.Count ?? 0 - contextCount, input.History?.Count ?? 0 - historyCount, currentTokenCount);
                            return Task.FromResult(output);
                        }

                        output.Context.Add(context);
                        currentTokenCount += tokenCount;
                    }

                    contextCount--;
                }
            }

            if (historyCount > 0)
            {
                var currentHistory = input.History?[output.History.Count];
                if (currentHistory is not null)
                {
                    int tokenCount = encoding.CountTokens($"{currentHistory.Role}: {currentHistory.Msg}");
                    if (currentTokenCount + tokenCount > contextWindowLimit)
                    {
                        this.LogDebug("context window reached at history index {context_index}, discarded {discard_context_count} context, {discard_history_count} history, current context window: {current_context_window}",
                          output.History.Count, input.Docs?.Count ?? 0 - contextCount, input.History?.Count ?? 0 - historyCount, currentTokenCount);
                        return Task.FromResult(output);
                    }
                    output.History.Add(currentHistory);
                    currentTokenCount += tokenCount;
                }
                historyCount--;
            }
        }

        return Task.FromResult(output);
    }

    private static Context GetContext(Doc doc, int index)
    {
        var chunk = "[ref" + index + "]\nTitle:" + doc.Title + "\n" + doc.Content + "\n[/ref" + index + "]";
        return new Context
        {
            Text = chunk,
            Id = "ref" + index,
            Title = doc.Title ?? doc.Urls?[0] ?? $"Document {index}",
            Uris = doc.GroundTruthUrls,
        };
    }
}