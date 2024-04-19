using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public abstract class BaseStep<TInput, TOutput>(ILogger logger) : IStep<TInput, TOutput>
{
    private readonly ILogger logger = logger;

    public abstract string Name { get; }

    public List<LogEntry> Logs => [];

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

    protected void LogError(Exception ex, string message)
    {
        this.logger.LogError(ex, message);
        this.Logs.Add(new LogEntry("ERROR", message));
    }

    public Task<TOutput> Execute(TInput input)
    {
        return this.ExecuteInternal(input);
    }

    public abstract Task<TOutput> ExecuteInternal(TInput input);
}