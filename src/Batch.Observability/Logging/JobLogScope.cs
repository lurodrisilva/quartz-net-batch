using Dynatrace.OneAgent.Sdk.Api;

namespace Batch.Observability.Logging;

// Structured log scope properties for correlation.
// When using Serilog or Microsoft.Extensions.Logging with BeginScope,
// these fields are automatically attached to every log entry within the scope.
// Dynatrace log ingestion uses dt.trace_id / dt.span_id for PurePath correlation.

public static class JobLogScope
{
    public static Dictionary<string, object> Create(
        string jobName,
        string jobGroup,
        string triggerName,
        string triggerGroup,
        string fireInstanceId,
        DateTimeOffset scheduledFireTime,
        IOneAgentSdk? sdk = null)
    {
        var scope = new Dictionary<string, object>
        {
            ["job.name"] = jobName,
            ["job.group"] = jobGroup,
            ["trigger.name"] = triggerName,
            ["trigger.group"] = triggerGroup,
            ["job.fireInstanceId"] = fireInstanceId,
            ["job.scheduledFireTime"] = scheduledFireTime.ToString("o")
        };

        // Inject Dynatrace trace context for log-to-trace correlation
        if (sdk is not null)
        {
            var traceCtx = sdk.TraceContextInfo;
            if (traceCtx.IsValid)
            {
                scope["dt.trace_id"] = traceCtx.TraceId;
                scope["dt.span_id"] = traceCtx.SpanId;
            }
        }

        return scope;
    }
}
