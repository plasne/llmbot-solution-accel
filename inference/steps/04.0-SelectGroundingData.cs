using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Inference;

public class SelectGroundingData(ILogger<SelectGroundingData> logger)
    : BaseStep<GroundingData, GroundingData>(logger)
{
    public override string Name => "SelectGroundingData";

    public override Task<GroundingData> ExecuteInternal(
        GroundingData input,
        CancellationToken cancellationToken = default)
    {
        GroundingData output = new() { UserQuery = input.UserQuery };

        // Some ideas on what to do here:
        // - trim
        // - summarize history
        // - re-rank

        // trim to top 5 documents and make them content
        if (input.Docs is not null)
        {
            var ordered = input.Docs.OrderByDescending(x => x.SearchScore);
            output.Context = [];
            foreach (var doc in ordered.Take(10))
            {
                int index = output.Context.Count;
                var chunk = "[ref" + index + "]\nTitle:" + doc.Title + "\n" + doc.Chunk + "\n[/ref" + index + "]";
                var context = new Context
                {
                    Id = "ref" + index,
                    Title = doc.Title ?? doc.Url ?? $"Document {index}",
                    Uri = doc.Url,
                    Text = chunk,
                };
                output.Context.Add(context);
            }
        }

        // trim to 5 turns of the conversation
        output.History = input.History?.Skip(Math.Max(0, input.History.Count - 5)).ToList();

        // NOTE: you could re-rank the documents here as well. GenerateAnswer should respect the re-ranking when
        // it emits citations. (ie. the citations are in order from most to least relevant.)

        return Task.FromResult(output);
    }
}