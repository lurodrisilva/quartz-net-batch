using System.Diagnostics;
using Batch.Domain.Constants;
using Batch.Domain.Interfaces;
using Batch.Observability.Logging;
using Dynatrace.OneAgent.Sdk.Api;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Batch.Application.Jobs;

public abstract class BaseTracedJob : IJob
{
    private readonly IJobTracer _tracer;
    private readonly IJobMetrics _metrics;
    private readonly IOneAgentSdk _sdk;
    private readonly ILogger _logger;
    protected ILogger Logger => _logger;

    protected BaseTracedJob(
        IJobTracer tracer,
        IJobMetrics metrics,
        IOneAgentSdk sdk,
        ILogger logger)
    {
        _tracer = tracer;
        _metrics = metrics;
        _sdk = sdk;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        string jobName = context.JobDetail.Key.Name;
        string jobGroup = context.JobDetail.Key.Group;
        string triggerName = context.Trigger.Key.Name;
        string triggerGroup = context.Trigger.Key.Group;
        string fireInstanceId = context.FireInstanceId;
        var scheduledFireTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

        var logScope = JobLogScope.Create(
            jobName, jobGroup, triggerName, triggerGroup, fireInstanceId, scheduledFireTime, _sdk);

        using var _ = Logger.BeginScope(logScope);
        using var traceScope = _tracer.StartJobTrace(jobName, jobGroup, fireInstanceId);

        var sw = Stopwatch.StartNew();
        _metrics.RecordJobStarted(jobName, jobGroup);

        Logger.LogInformation("Job execution started");

        try
        {
            await ExecuteJob(context);

            sw.Stop();
            _metrics.RecordJobSucceeded(jobName, jobGroup);
            _metrics.RecordJobDuration(jobName, jobGroup, sw.Elapsed.TotalMilliseconds);
            traceScope.SetAttribute("job.outcome", "success");

            Logger.LogInformation("Job execution completed in {DurationMs:F1}ms", sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex) when (ex is not JobExecutionException)
        {
            sw.Stop();
            HandleFailure(ex, jobName, jobGroup, sw.Elapsed.TotalMilliseconds, traceScope, context);
            throw new JobExecutionException(ex, refireImmediately: false);
        }
        catch (JobExecutionException ex)
        {
            sw.Stop();
            HandleFailure(ex.InnerException ?? ex, jobName, jobGroup, sw.Elapsed.TotalMilliseconds, traceScope, context);
            throw;
        }
    }

    protected abstract Task ExecuteJob(IJobExecutionContext context);

    private void HandleFailure(
        Exception ex,
        string jobName,
        string jobGroup,
        double durationMs,
        IJobTraceScope traceScope,
        IJobExecutionContext context)
    {
        string exceptionType = ex.GetType().Name;

        _metrics.RecordJobFailed(jobName, jobGroup, exceptionType);
        _metrics.RecordJobDuration(jobName, jobGroup, durationMs);
        traceScope.SetError(ex);
        traceScope.SetAttribute("job.outcome", "failed");
        traceScope.SetAttribute("exception.type", exceptionType);

        int retryCount = context.MergedJobDataMap.ContainsKey(JobConstants.RetryCountKey)
            ? context.MergedJobDataMap.GetIntValue(JobConstants.RetryCountKey)
            : 0;

        Logger.LogError(ex,
            "Job execution failed after {DurationMs:F1}ms (retry #{RetryCount})",
            durationMs, retryCount);
    }
}
