using Batch.Domain.Constants;
using Batch.Domain.Interfaces;
using Dynatrace.OneAgent.Sdk.Api;
using Dynatrace.OneAgent.Sdk.Api.Enums;
using Microsoft.Extensions.Logging;

namespace Batch.Observability.Tracing;

// Dynatrace OneAgent auto-instruments: ADO.NET/SqlClient, HttpClient, ASP.NET Core requests.
// Manual instrumentation via OneAgent SDK is needed for:
//   - Batch job execution boundaries (Quartz fires jobs on thread-pool threads)
//   - In-process thread handoff (Quartz scheduler → job thread)
//   - Custom request attributes for job metadata
// This service wraps the OneAgent SDK to trace job executions as "incoming remote calls"
// so they appear as service entry points in Dynatrace PurePath.

public sealed partial class DynatraceTracingService : IJobTracer
{
    private readonly IOneAgentSdk _sdk;
    private readonly ILogger<DynatraceTracingService> _logger;

    public DynatraceTracingService(IOneAgentSdk sdk, ILogger<DynatraceTracingService> logger)
    {
        _sdk = sdk;
        _logger = logger;

        if (_sdk.CurrentState == SdkState.PERMANENTLY_INACTIVE)
        {
            LogSdkInactive(_logger);
        }
    }

    public IJobTraceScope StartJobTrace(string jobName, string jobGroup, string fireInstanceId)
    {
        // TraceIncomingRemoteCall creates a PurePath entry point for this job execution.
        // This is the correct pattern for background work that has no incoming HTTP request.
        var tracer = _sdk.TraceIncomingRemoteCall(
            serviceMethod: $"{jobGroup}.{jobName}",
            serviceName: JobConstants.DynatraceServiceName,
            serviceEndpoint: JobConstants.DynatraceServiceEndpoint);

        tracer.SetProtocolName("Quartz.NET");

        tracer.Start();

        // Attach custom request attributes for filtering/searching in Dynatrace
        _sdk.AddCustomRequestAttribute("job.name", jobName);
        _sdk.AddCustomRequestAttribute("job.group", jobGroup);
        _sdk.AddCustomRequestAttribute("job.fireInstanceId", fireInstanceId);

        return new DynatraceJobTraceScope(tracer, _sdk);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Dynatrace OneAgent SDK is permanently inactive. " +
                  "Tracing calls will be no-ops. Ensure OneAgent is installed and Deep Monitoring is enabled")]
    private static partial void LogSdkInactive(ILogger logger);
}

internal sealed class DynatraceJobTraceScope : IJobTraceScope
{
    private readonly IIncomingRemoteCallTracer _tracer;
    private readonly IOneAgentSdk _sdk;
    private bool _disposed;

    public DynatraceJobTraceScope(IIncomingRemoteCallTracer tracer, IOneAgentSdk sdk)
    {
        _tracer = tracer;
        _sdk = sdk;
    }

    public void SetError(Exception exception)
    {
        _tracer.Error(exception);
    }

    public void SetAttribute(string key, string value)
    {
        _sdk.AddCustomRequestAttribute(key, value);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tracer.End();
    }
}
