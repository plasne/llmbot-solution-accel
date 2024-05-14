using System.Threading.Tasks;
using AdaptiveCards.Templating;

namespace Bot;

public interface ICardProvider
{
    public Task<AdaptiveCardTemplate> GetTemplate(string name);
}