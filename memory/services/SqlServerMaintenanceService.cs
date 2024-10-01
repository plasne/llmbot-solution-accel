using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Memory;

public class SqlServerMaintenanceService : BackgroundService
{
    private readonly IConfig config;
    private readonly SqlServerMemoryStore sqlServerMemoryStore;
    private readonly ILogger<SqlServerMaintenanceService> logger;

    public SqlServerMaintenanceService(IConfig config, IMemoryStore memoryStore, ILogger<SqlServerMaintenanceService> logger)
    {
        if (memoryStore is not SqlServerMemoryStore store)
        {
            throw new Exception("SqlServerMaintenanceService can only be used in conjuction with SqlServerMemoryStore.");
        }
        this.config = config;
        this.sqlServerMemoryStore = store;
        this.logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.sqlServerMemoryStore.ProvisionAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (this.config.RUN_RETENTION_EVERY_X_HOURS <= 0)
        {
            this.logger.LogInformation("retention is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(this.config.RUN_RETENTION_EVERY_X_HOURS), stoppingToken);
            try
            {
                await this.sqlServerMemoryStore.DeleteExpiredAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "there was an error during the retention step in SqlServerMaintenanceService...");
                // continue
            }
        }
    }
}