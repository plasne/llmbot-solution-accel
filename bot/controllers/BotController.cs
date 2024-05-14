using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace Bot;

[Route("api/messages")]
[ApiController]
public class BotController(IBotFrameworkHttpAdapter adapter, IBot bot) : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter adapter = adapter;
    private readonly IBot bot = bot;

    [HttpPost]
    [HttpGet]
    public Task PostAsync()
    {
        return adapter.ProcessAsync(Request, Response, bot);
    }
}
