using Batch.Domain.Interfaces;
using Dynatrace.OneAgent.Sdk.Api;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Batch.Application.Jobs;

// This job demonstrates a one-time/manual execution pattern.
// RequestsRecovery = true means if the scheduler crashes mid-execution,
// this job will be re-executed on scheduler restart.

[PersistJobDataAfterExecution]
public sealed class SampleOneTimeJob : BaseTracedJob
{
    public SampleOneTimeJob(
        IJobTracer tracer,
        IJobMetrics metrics,
        IOneAgentSdk sdk,
        ILogger<SampleOneTimeJob> logger)
        : base(tracer, metrics, sdk, logger)
    {
    }

    protected override async Task ExecuteJob(IJobExecutionContext context)
    {
        string targetId = context.MergedJobDataMap.GetString("TargetId") ?? "unknown";

        Logger.LogInformation("Executing one-time job for target {TargetId}", targetId);

        await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);

        context.JobDetail.JobDataMap.Put("CompletedAt", DateTimeOffset.UtcNow.ToString("o"));
        context.JobDetail.JobDataMap.Put("Status", "Completed");

        Logger.LogInformation("One-time job completed for target {TargetId}", targetId);
    }
}
