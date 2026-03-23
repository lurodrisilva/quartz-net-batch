using Batch.Domain.Interfaces;
using Dynatrace.OneAgent.Sdk.Api;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Batch.Application.Jobs;

// DisallowConcurrentExecution: prevents overlapping runs when a job takes longer
// than the trigger interval. Quartz will delay the next fire until the current
// execution completes. Essential for data-integrity-sensitive batch operations.
//
// PersistJobDataAfterExecution: saves changes to JobDataMap after each run,
// enabling stateful tracking (e.g., retry count, watermarks).

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public sealed class SampleRecurringJob : BaseTracedJob
{
    public SampleRecurringJob(
        IJobTracer tracer,
        IJobMetrics metrics,
        IOneAgentSdk sdk,
        ILogger<SampleRecurringJob> logger)
        : base(tracer, metrics, sdk, logger)
    {
    }

    protected override async Task ExecuteJob(IJobExecutionContext context)
    {
        int batchSize = context.MergedJobDataMap.GetInt("BatchSize");
        if (batchSize <= 0) batchSize = 100;

        Logger.LogInformation("Processing batch of {BatchSize} items", batchSize);

        for (int i = 0; i < batchSize; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            await ProcessItemAsync(i);

            if (i > 0 && i % 25 == 0)
            {
                Logger.LogDebug("Processed {Count}/{Total} items", i, batchSize);
            }
        }

        context.JobDetail.JobDataMap.Put("LastProcessedCount", batchSize);
        context.JobDetail.JobDataMap.Put("LastRunTime", DateTimeOffset.UtcNow.ToString("o"));

        Logger.LogInformation("Batch completed: {ItemCount} items processed", batchSize);
    }

    private static async Task ProcessItemAsync(int itemIndex)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(10));
    }
}
