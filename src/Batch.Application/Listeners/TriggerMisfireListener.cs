using Batch.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;

namespace Batch.Application.Listeners;

public sealed class TriggerMisfireListener : TriggerListenerSupport
{
    private readonly IJobMetrics _metrics;
    private readonly ILogger<TriggerMisfireListener> _logger;

    public override string Name => "GlobalTriggerMisfireListener";

    public TriggerMisfireListener(IJobMetrics metrics, ILogger<TriggerMisfireListener> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    public override Task TriggerMisfired(ITrigger trigger, CancellationToken cancellationToken = default)
    {
        string jobName = trigger.JobKey.Name;
        string jobGroup = trigger.JobKey.Group;

        _metrics.RecordMisfire(jobName, jobGroup);

        _logger.LogWarning(
            "Trigger misfired: {TriggerKey} for job {JobKey}. " +
            "MisfireInstruction={MisfireInstruction}, NextFireTime={NextFireTime}",
            trigger.Key, trigger.JobKey,
            trigger.MisfireInstruction,
            trigger.GetNextFireTimeUtc());

        return Task.CompletedTask;
    }
}
