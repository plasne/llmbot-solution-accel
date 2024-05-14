using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Memory;

public class SqlServerMaintenanceService : BackgroundService
{
    private readonly SqlServerMemoryStore sqlServerMemoryStore;

    public SqlServerMaintenanceService(IMemoryStore memoryStore)
    {
        if (memoryStore is not SqlServerMemoryStore store)
        {
            throw new Exception("SqlServerMaintenanceService can only be used in conjuction with SqlServerMemoryStore.");
        }
        this.sqlServerMemoryStore = store;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.sqlServerMemoryStore.ProvisionAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // start the retention loop
        return Task.CompletedTask;
    }
}