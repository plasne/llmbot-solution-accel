using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Inference;

public abstract class BicycleShopSearchBaseStep<TInput, TOutput>(ILogger logger) : BaseStep<TInput, TOutput>(logger)
{
    protected Dictionary<string, string> BicyleDocs { get; } = new Dictionary<string, string>{
        {"https://my-bicycle-shop/about", "My Bicycle Shop was founded in 2010 by John Doe."},
        {"https://my-bicycle-shop/contact", "You can contact us at 555-555-5555."},
        {"https://my-bicycle-shop/products", "We sell bicycles, helmets, and other accessories."},
        {"https://my-bicycle-shop/locations", "We have locations in Seattle, Portland, and San Francisco."},
    };
}