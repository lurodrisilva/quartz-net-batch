using Batch.Application.Jobs;
using Batch.Application.Listeners;
using Batch.Domain.Constants;
using Batch.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Batch.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddBatchInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var quartzSettings = new QuartzSettings();
        configuration.GetSection(QuartzSettings.SectionName).Bind(quartzSettings);

        var scheduleSettings = new JobScheduleSettings();
        configuration.GetSection(JobScheduleSettings.SectionName).Bind(scheduleSettings);

        services.Configure<QuartzSettings>(configuration.GetSection(QuartzSettings.SectionName));
        services.Configure<JobScheduleSettings>(configuration.GetSection(JobScheduleSettings.SectionName));

        services.AddQuartz(q =>
        {
            q.SchedulerName = quartzSettings.SchedulerName;
            q.SchedulerId = quartzSettings.InstanceId;
            q.MaxBatchSize = quartzSettings.MaxBatchSize;

            q.InterruptJobsOnShutdown = true;
            q.InterruptJobsOnShutdownWithWait = true;

            q.UseSimpleTypeLoader();
            q.UseDefaultThreadPool(tp => tp.MaxConcurrency = quartzSettings.ThreadPoolSize);

            // JobStoreTX with SQL Server — stores all scheduler metadata in Azure SQL Database.
            // This enables: cluster mode, job recovery, persistent schedules across restarts.
            q.UsePersistentStore(store =>
            {
                store.PerformSchemaValidation = quartzSettings.PerformSchemaValidation;
                store.UseProperties = true;
                store.RetryInterval = TimeSpan.FromSeconds(15);

                store.UseSqlServer(sqlServer =>
                {
                    sqlServer.ConnectionString =
                        configuration.GetConnectionString("QuartzDatabase")
                        ?? throw new InvalidOperationException(
                            "Connection string 'QuartzDatabase' is required for JobStoreTX");
                    sqlServer.TablePrefix = quartzSettings.TablePrefix;
                });

                // System.Text.Json serializer for JobDataMap persistence.
                // Preferred over BinaryFormatter (security) and Newtonsoft (dependency).
                store.UseSystemTextJsonSerializer();

                if (quartzSettings.Clustered)
                {
                    // Clustering allows multiple scheduler instances (AKS pods) to share
                    // the same job store. Only one instance fires a given trigger at a time.
                    // CheckinInterval controls how often instances heartbeat to the DB.
                    // CheckinMisfireThreshold controls when a missed checkin marks an instance as failed.
                    store.UseClustering(cluster =>
                    {
                        cluster.CheckinInterval = TimeSpan.FromMilliseconds(quartzSettings.ClusterCheckinIntervalMs);
                        cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                    });
                }
            });

            RegisterJobs(q, scheduleSettings);
            RegisterListeners(q);
        });

        // WaitForJobsToComplete ensures graceful shutdown — the hosted service
        // waits for running jobs to finish before the process exits.
        // StartDelay gives time for DI container and health checks to initialize.
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
            options.StartDelay = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    private static void RegisterJobs(IServiceCollectionQuartzConfigurator q, JobScheduleSettings settings)
    {
        if (settings.RecurringJob.Enabled)
        {
            var jobKey = new JobKey(JobConstants.SampleRecurringJobName, JobConstants.RecurringJobGroup);

            q.AddJob<SampleRecurringJob>(j => j
                .WithIdentity(jobKey)
                .WithDescription("Sample recurring batch job that processes items on a cron schedule")
                .StoreDurably()
                .RequestRecovery()
                .UsingJobData("BatchSize", settings.RecurringJob.BatchSize)
                .UsingJobData(JobConstants.MaxRetriesKey, settings.RecurringJob.MaxRetries)
                .UsingJobData(JobConstants.RetryCountKey, 0));

            // Cron trigger: WithMisfireHandlingInstructionFireAndProceed means
            // if the scheduler missed a fire time (e.g., pod restart), it fires
            // once immediately and then resumes the normal schedule.
            q.AddTrigger(t => t
                .WithIdentity(JobConstants.SampleRecurringTriggerName, JobConstants.RecurringTriggerGroup)
                .ForJob(jobKey)
                .WithCronSchedule(settings.RecurringJob.CronExpression, cron => cron
                    .InTimeZone(TimeZoneInfo.Utc)
                    .WithMisfireHandlingInstructionFireAndProceed())
                .WithDescription("Cron trigger for recurring batch job")
                .StartNow());
        }

        if (settings.OneTimeJob.Enabled)
        {
            var jobKey = new JobKey(JobConstants.SampleOneTimeJobName, JobConstants.ManualJobGroup);

            q.AddJob<SampleOneTimeJob>(j => j
                .WithIdentity(jobKey)
                .WithDescription("Sample one-time job triggered manually or on startup")
                .StoreDurably()
                .RequestRecovery()
                .UsingJobData("TargetId", "startup-sample"));

            // Simple trigger: fires once after a delay with no repeat.
            // WithMisfireHandlingInstructionFireNow means if missed, fire immediately.
            q.AddTrigger(t => t
                .WithIdentity(JobConstants.SampleOneTimeTriggerName, JobConstants.ManualTriggerGroup)
                .ForJob(jobKey)
                .StartAt(DateBuilder.FutureDate(settings.OneTimeJob.DelaySeconds, IntervalUnit.Second))
                .WithSimpleSchedule(s => s
                    .WithRepeatCount(0)
                    .WithMisfireHandlingInstructionFireNow())
                .WithDescription("One-time trigger that fires once after startup delay"));
        }
    }

    private static void RegisterListeners(IServiceCollectionQuartzConfigurator q)
    {
        q.AddJobListener<JobExecutionListener>();
        q.AddTriggerListener<TriggerMisfireListener>();
    }
}
