using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Search.Documents.Indexes.Models;

namespace Inference;

public class HardcodedBicycleSearchService() : ISearchService
{
    private readonly Dictionary<string, string> docs = new Dictionary<string, string>{
        {"https://my-bicycle-shop/about", "My Bicycle Shop was founded in 2010 by John Doe."},
        {"https://my-bicycle-shop/contact", "You can contact us at 555-555-5555."},
        {"https://my-bicycle-shop/products", "We sell bicycles, helmets, and other accessories."},
        {"https://my-bicycle-shop/locations", "We have locations in Seattle, Portland, and San Francisco."},
    };

    public Task<IList<Doc>> GetDocumentsAsync(string text, CancellationToken cancellationToken = default)
    {
        List<Doc> docs = [];
        foreach (var part in text.Split(" OR "))
        {
            var kv = part.Split(":", 2);
            if (kv.Length == 2)
            {
                var uri = kv[1].Trim('"');
                if (this.docs.ContainsKey(uri))
                {
                    docs.Add(new Doc
                    {
                        Title = uri,
                        Urls = [uri],
                        Content = this.docs[uri],
                    });
                }
            }
        }
        return Task.FromResult<IList<Doc>>(docs);
    }

    public Task<IList<Doc>> SearchAsync(string text, CancellationToken cancellationToken = default)
    {
        List<Doc> docs = [];
        var keywords = text.Split(" ").Select(x => x.ToLower());
        foreach (var doc in this.docs)
        {
            var srcwords = doc.Value.Split(" ").Select(x => x.ToLower());
            if (keywords.Any(keyword => srcwords.Contains(keyword)))
            {
                docs.Add(new Doc
                {
                    Title = doc.Key,
                    Urls = [doc.Key],
                    Content = doc.Value,
                });
            }
        }
        return Task.FromResult<IList<Doc>>(docs);
    }
}