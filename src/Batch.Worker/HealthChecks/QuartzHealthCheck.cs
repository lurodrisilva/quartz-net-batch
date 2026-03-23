using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

namespace Batch.Worker.HealthChecks;

public sealed class QuartzHealthCheck : IHealthCheck
{
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzHealthCheck(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            if (scheduler.IsShutdown)
            {
                return HealthCheckResult.Unhealthy("Scheduler is shut down");
            }

            if (!scheduler.IsStarted)
            {
                return HealthCheckResult.Degraded("Scheduler has not started yet");
            }

            if (scheduler.InStandbyMode)
            {
                return HealthCheckResult.Degraded("Scheduler is in standby mode");
            }

            var metadata = await scheduler.GetMetaData(cancellationToken);
            var data = new Dictionary<string, object>
            {
                ["schedulerName"] = metadata.SchedulerName,
                ["schedulerInstanceId"] = metadata.SchedulerInstanceId,
                ["threadPoolSize"] = metadata.ThreadPoolSize,
                ["jobsExecuted"] = metadata.NumberOfJobsExecuted,
                ["runningSince"] = metadata.RunningSince?.ToString("o") ?? "N/A",
                ["inStandbyMode"] = metadata.InStandbyMode,
                ["schedulerType"] = metadata.SchedulerType.Name
            };

            return HealthCheckResult.Healthy("Scheduler is running", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Scheduler health check failed", ex);
        }
    }
}
