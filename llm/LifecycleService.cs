using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class LifecycleService : IHostedService
{
    private readonly IConfig config;

    public LifecycleService(IConfig config)
    {
        this.config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        config.Validate();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}