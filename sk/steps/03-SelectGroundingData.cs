using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class SelectGroundingData(ILogger<SelectGroundingData> logger)
    : BaseStep<GroundingData, GroundingData>(logger)
{
    public override string Name => "SelectGroundingData";

    public override Task<GroundingData> ExecuteInternal(GroundingData input)
    {
        var output = new GroundingData();

        // Some ideas on what to do here:
        // - trim
        // - summarize history
        // - re-rank

        // trim to top 5 documents and make them content
        if (input.Docs is not null)
        {
            output.Content = [];
            foreach (var doc in input.Docs.Take(5))
            {
                int index = output.Content.Count;
                var chunk = "[doc" + index + "]\nTitle:" + doc.Title + "\n" + doc.Chunk + "\n[/doc" + index + "]";
                output.Content.Add(chunk);
            }
        }

        // trim to 5 turns of the conversation
        output.History = input.History?.Skip(Math.Max(0, input.History.Count() - 5)).ToList();

        return Task.FromResult(output);
    }
}