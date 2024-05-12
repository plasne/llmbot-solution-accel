using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Microsoft.SemanticKernel;

namespace Shared;

public static class Extensions
{
    public static void AddOpenTelemetry(
        this ILoggingBuilder builder,
        string openTelemetryConnectionString)
    {
        builder.AddOpenTelemetry(logging =>
        {
            logging.AddAzureMonitorLogExporter(o => o.ConnectionString = openTelemetryConnectionString);
        });
    }

    public static void AddOpenTelemetry(
        this IServiceCollection serviceCollection,
        string sourceName, string applicationName,
        string openTelemetryConnectionString)
    {
        serviceCollection.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: applicationName))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();

                // Metrics provides by ASP.NET Core in .NET 8
                metrics.AddMeter("Microsoft.AspNetCore.Hosting");
                metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                metrics.AddMeter(sourceName);
                metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = openTelemetryConnectionString);
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();

                tracing.AddSource(sourceName);

                // exports
                tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = openTelemetryConnectionString);
            });
    }

    public static void AddOpenTelemetry(
        this IKernelBuilder kernelBuilder,
        string applicationName,
        string openTelemetryConnectionString)
    {
        var loggerFactory = LoggerFactory.Create(config =>
        {
            // Add OpenTelemetry as a logging provider
            config.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(applicationName));

                options.AddAzureMonitorLogExporter(o => o.ConnectionString = openTelemetryConnectionString);

                options.IncludeScopes = true;
                // Format log messages. This defaults to false.
                options.IncludeFormattedMessage = true;
            });
        });

        kernelBuilder.Services.AddSingleton(loggerFactory);
    }
}
