using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class SqlServerMaintenanceService : BackgroundService
{
    private readonly SqlServerMemoryStore sqlServerMemoryStore;
    private readonly ILogger<SqlServerMaintenanceService> logger;

    public SqlServerMaintenanceService(
        IMemoryStore memoryStore,
        ILogger<SqlServerMaintenanceService> logger)
    {
        if (memoryStore is not SqlServerMemoryStore sqlServerMemoryStore)
        {
            throw new Exception("SqlServerMaintenanceService can only be used in conjuction with SqlServerMemoryStore.");
        }
        this.sqlServerMemoryStore = sqlServerMemoryStore;
        this.logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.sqlServerMemoryStore.ProvisionAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // start the retention loop
        // TODO: implement this
        /*
        while (!stoppingToken.IsCancellationRequested)
        {
            // do stuff
        }
        */
        return Task.CompletedTask;
    }
}