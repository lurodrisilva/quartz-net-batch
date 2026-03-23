using System.Diagnostics.Metrics;
using Batch.Domain.Constants;
using Batch.Domain.Interfaces;

namespace Batch.Observability.Metrics;

// Uses System.Diagnostics.Metrics which Dynatrace OneAgent collects automatically
// when running with the .NET runtime instrumentation.
// Metric names follow OpenTelemetry semantic conventions where applicable.

public sealed class JobMetricsService : IJobMetrics
{
    private readonly Counter<long> _jobsStarted;
    private readonly Counter<long> _jobsSucceeded;
    private readonly Counter<long> _jobsFailed;
    private readonly Histogram<double> _jobDuration;
    private readonly Counter<long> _jobRetries;
    private readonly Counter<long> _misfires;

    public JobMetricsService(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(JobConstants.MeterName);

        _jobsStarted = meter.CreateCounter<long>(
            "batch.job.started",
            unit: "{job}",
            description: "Number of job executions started");

        _jobsSucceeded = meter.CreateCounter<long>(
            "batch.job.succeeded",
            unit: "{job}",
            description: "Number of job executions that completed successfully");

        _jobsFailed = meter.CreateCounter<long>(
            "batch.job.failed",
            unit: "{job}",
            description: "Number of job executions that failed");

        _jobDuration = meter.CreateHistogram<double>(
            "batch.job.duration",
            unit: "ms",
            description: "Duration of job execution in milliseconds");

        _jobRetries = meter.CreateCounter<long>(
            "batch.job.retries",
            unit: "{retry}",
            description: "Number of job retry attempts");

        _misfires = meter.CreateCounter<long>(
            "batch.job.misfires",
            unit: "{misfire}",
            description: "Number of trigger misfires");
    }

    public void RecordJobStarted(string jobName, string jobGroup) =>
        _jobsStarted.Add(1, Tag("job.name", jobName), Tag("job.group", jobGroup));

    public void RecordJobSucceeded(string jobName, string jobGroup) =>
        _jobsSucceeded.Add(1, Tag("job.name", jobName), Tag("job.group", jobGroup));

    public void RecordJobFailed(string jobName, string jobGroup, string exceptionType) =>
        _jobsFailed.Add(1, Tag("job.name", jobName), Tag("job.group", jobGroup), Tag("exception.type", exceptionType));

    public void RecordJobDuration(string jobName, string jobGroup, double durationMs) =>
        _jobDuration.Record(durationMs, Tag("job.name", jobName), Tag("job.group", jobGroup));

    public void RecordJobRetry(string jobName, string jobGroup) =>
        _jobRetries.Add(1, Tag("job.name", jobName), Tag("job.group", jobGroup));

    public void RecordMisfire(string jobName, string jobGroup) =>
        _misfires.Add(1, Tag("job.name", jobName), Tag("job.group", jobGroup));

    private static KeyValuePair<string, object?> Tag(string key, string value) => new(key, value);
}
