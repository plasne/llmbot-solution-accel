using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class LifecycleService(IHistoryService historyService) : IHostedService
{
    private readonly IHistoryService historyService = historyService;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.historyService.StartupAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}