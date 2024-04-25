using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public abstract class BaseStep<TInput, TOutput>(ILogger logger) : IStep<TInput, TOutput>
{
    private readonly ILogger logger = logger;

    public abstract string Name { get; }

    public List<LogEntry> Logs { get; } = [];

    protected void LogDebug(string message)
    {
        this.logger.LogDebug(message);
        this.Logs.Add(new LogEntry("DEBUG", message));
    }

    protected void LogInformation(string message)
    {
        this.logger.LogInformation(message);
        this.Logs.Add(new LogEntry("INFO", message));
    }

    protected void LogWarning(string message)
    {
        this.logger.LogWarning(message);
        this.Logs.Add(new LogEntry("WARNING", message));
    }

    protected void LogError(Exception ex, string message)
    {
        this.logger.LogError(ex, message);
        this.Logs.Add(new LogEntry("ERROR", message + ": " + ex.Message));
    }

    public Task<TOutput> Execute(TInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<TOutput>(cancellationToken);
            }

            return this.ExecuteInternal(input, cancellationToken);
        }
        catch (Exception ex)
        {
            this.Logs.Add(new LogEntry("ERROR", ex.Message));
            throw;
        }
    }

    public abstract Task<TOutput> ExecuteInternal(TInput input, CancellationToken cancellationToken = default);
}