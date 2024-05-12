using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DistributedChat;
using Microsoft.Extensions.Logging;

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
            foreach (var doc in input.Docs.Take(10))
            {
                int index = output.Context.Count;
                var chunk = "[ref" + index + "]\nTitle:" + doc.Title + "\n" + doc.Chunk + "\n[/ref" + index + "]";
                var context = new Context
                {
                    Text = chunk,
                    Citation = new Citation
                    {
                        Ref = "ref" + index,
                        Title = doc.Title,
                        Uri = "https://" + doc.Title // update to some kind of URL
                    }
                };
                output.Context.Add(context);
            }
        }

        // trim to 5 turns of the conversation
        output.History = input.History?.Skip(Math.Max(0, input.History.Count() - 5)).ToList();

        return Task.FromResult(output);
    }
}