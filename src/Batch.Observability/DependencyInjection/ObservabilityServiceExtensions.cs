using Batch.Domain.Interfaces;
using Batch.Observability.Metrics;
using Batch.Observability.Tracing;
using Dynatrace.OneAgent.Sdk.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Batch.Observability.DependencyInjection;

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddBatchObservability(this IServiceCollection services)
    {
        // Dynatrace OneAgent SDK — singleton; safe to create once and reuse.
        // When no OneAgent is present the SDK returns a no-op implementation automatically.
        services.AddSingleton<IOneAgentSdk>(sp =>
        {
            var sdk = OneAgentSdkFactory.CreateInstance();
            var logCallback = sp.GetRequiredService<DynatraceSdkLogCallback>();
            sdk.SetLoggingCallback(logCallback);
            return sdk;
        });

        services.AddSingleton<DynatraceSdkLogCallback>();
        services.AddSingleton<IJobTracer, DynatraceTracingService>();
        services.AddSingleton<IJobMetrics, JobMetricsService>();

        return services;
    }
}
