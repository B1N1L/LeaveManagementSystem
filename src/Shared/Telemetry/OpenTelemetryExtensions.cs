using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Telemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddDistributedTracing(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var jaegerEndpoint = configuration["Jaeger:Endpoint"]
            ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    // Identify this service in Jaeger
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName))

                    // Auto-instrument incoming HTTP requests
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })

                    // Auto-instrument outgoing HTTP calls
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })

                    // Send traces to Jaeger via OTLP
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(jaegerEndpoint);
                    });
            });

        return services;
    }
}