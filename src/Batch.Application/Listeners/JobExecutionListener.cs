using Batch.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;

namespace Batch.Application.Listeners;

public sealed class JobExecutionListener : JobListenerSupport
{
    private readonly IJobMetrics _metrics;
    private readonly ILogger<JobExecutionListener> _logger;

    public override string Name => "GlobalJobExecutionListener";

    public JobExecutionListener(IJobMetrics metrics, ILogger<JobExecutionListener> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    public override Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Job about to execute: {JobKey} via trigger {TriggerKey}",
            context.JobDetail.Key, context.Trigger.Key);
        return Task.CompletedTask;
    }

    public override Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        if (jobException is not null)
        {
            _logger.LogWarning(
                "Job {JobKey} completed with exception: {ExceptionMessage}",
                context.JobDetail.Key, jobException.Message);
        }

        return Task.CompletedTask;
    }

    public override Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Job execution vetoed: {JobKey}", context.JobDetail.Key);
        return Task.CompletedTask;
    }
}
