using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Inference;

public abstract class BaseStep<TInput, TOutput> : IStep<TInput, TOutput>
{
    private readonly ILogger logger;

    public BaseStep(ILogger logger)
    {
        this.logger = logger;
        this.StepResponse = new WorkflowStepResponse<TInput, TOutput>(this.Logs, this.Usage);
    }

    public abstract string Name { get; }

    public List<LogEntry> Logs { get; } = [];

    public Usage Usage { get; } = new();

    public WorkflowStepResponse<TInput, TOutput> StepResponse { get; }

    protected void LogDebug(string message, params object?[] args)
    {
#pragma warning disable CA2254 // The logging message template should not vary between calls
        this.logger.LogDebug(message, args);
#pragma warning restore CA2254 // Restore the warning after this line
        this.Logs.Add(new LogEntry("DEBUG", args is not null && args.Length > 0 ? ToMessage(message, args) : message));
    }

    private static string ToMessage(string message, object?[] args)
    {
        StringBuilder sb = new();
        int messageIndexPtr = 0;

        int argCounter = 0;
        while (messageIndexPtr < message.Length)
        {
            int startIndex = message.IndexOf('{', messageIndexPtr);
            if (startIndex < 0)
            {
                if (messageIndexPtr < message.Length)
                {
                    sb.Append(message.AsSpan(messageIndexPtr));
                }
                break;
            }

            sb.Append(message.AsSpan(messageIndexPtr, startIndex - messageIndexPtr));
            sb.Append('{');
            messageIndexPtr = startIndex + 1;
            sb.Append(argCounter);
            argCounter++;
            int endIndex = message.IndexOf('}', messageIndexPtr);
            if (endIndex < 0)
            {
                // not sure why we are missing an enclosing }, return message as-is which is better than an exception since this is doing logging.
                return message;
            }
            sb.Append('}');
            messageIndexPtr = endIndex + 1;
        }
        return argCounter == args.Length ? string.Format(sb.ToString(), args) : message;
    }

    protected void LogInformation(string message)
    {
#pragma warning disable CA2254 // The logging message template should not vary between calls
        this.logger.LogInformation(message);
#pragma warning restore CA2254 // Restore the warning after this line
        this.Logs.Add(new LogEntry("INFO", message));
    }

    protected void LogWarning(string message)
    {
#pragma warning disable CA2254 // The logging message template should not vary between calls
        this.logger.LogWarning(message);
#pragma warning restore CA2254 // Restore the warning after this line
        this.Logs.Add(new LogEntry("WARNING", message));
    }

    protected void LogError(Exception ex, string message)
    {
#pragma warning disable CA2254 // The logging message template should not vary between calls
        this.logger.LogError(ex, message);
#pragma warning restore CA2254 // Restore the warning after this line
        this.Logs.Add(new LogEntry("ERROR", message + ": " + ex.Message));
    }

    public bool Continue
    {
        get => this.StepResponse.Continue;
        set => this.StepResponse.Continue = value;
    }

    public async Task<TOutput> Execute(TInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            // check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            // set INPUT step parameters
            this.StepResponse.Name = this.Name;
            this.StepResponse.Input = input;

            // start an activity for the step
            using var activity = DiagnosticService.Source.StartActivity(this.Name);
            if (activity is not null)
            {
                activity.SetBaggage("step", this.Name);
                foreach (var baggage in activity.Baggage)
                {
                    activity.SetTag(baggage.Key, baggage.Value);
                }
            }

            // execute the step
            var watch = Stopwatch.StartNew();
            var output = await this.ExecuteInternal(input, cancellationToken);
            watch.Stop();
            this.Usage.ExecutionTime = watch.ElapsedMilliseconds;

            // set OUTPUT step parameters
            this.StepResponse.Output = output;

            return output;
        }
        catch (Exception ex)
        {
            this.Logs.Add(new LogEntry("ERROR", ex.Message));
            throw;
        }
    }

    public abstract Task<TOutput> ExecuteInternal(TInput input, CancellationToken cancellationToken = default);
}