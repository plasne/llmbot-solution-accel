using System.Threading.Tasks;
using AdaptiveCards.Templating;

public interface ICardProvider
{
    public Task<AdaptiveCardTemplate> GetTemplate(string name);
}