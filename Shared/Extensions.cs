using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Microsoft.SemanticKernel;

namespace Shared;

public static class Extensions
{
    public static void AddOpenTelemetry(this WebApplicationBuilder builder, string sourceName, string openTelemetryConnectioString)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.AddAzureMonitorLogExporter(o => o.ConnectionString = openTelemetryConnectioString);
        });
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: builder.Environment.ApplicationName))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();

                // Metrics provides by ASP.NET Core in .NET 8
                metrics.AddMeter("Microsoft.AspNetCore.Hosting");
                metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");

                metrics.AddConsoleExporter();
                metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = openTelemetryConnectioString);
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();

                tracing.AddSource(sourceName);

                // exports
                tracing.AddConsoleExporter();
                tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = openTelemetryConnectioString);
            });
    }

    public static void AddOpenTelemetry(this IKernelBuilder kernelBuilder, string applicationName, string openTelemetryConnectioString)
    {
        var loggerFactory = LoggerFactory.Create(config =>
        {
            // Add OpenTelemetry as a logging provider
            config.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(applicationName));

                options.AddAzureMonitorLogExporter(o => o.ConnectionString = openTelemetryConnectioString);

                options.IncludeScopes = true;
                // Format log messages. This defaults to false.
                options.IncludeFormattedMessage = true;
            });
        });

        kernelBuilder.Services.AddSingleton(loggerFactory);
    }
}
