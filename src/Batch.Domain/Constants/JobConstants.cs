namespace Batch.Domain.Constants;

/// <summary>
/// Well-known job and trigger identifiers used throughout the application.
/// Centralizing these prevents magic string drift across configuration and code.
/// </summary>
public static class JobConstants
{
    // ─── Scheduler ────────────────────────────────────────────────────
    public const string SchedulerName = "BatchScheduler";

    // ─── Job Groups ───────────────────────────────────────────────────
    public const string RecurringJobGroup = "RecurringJobs";
    public const string ManualJobGroup = "ManualJobs";

    // ─── Job Names ────────────────────────────────────────────────────
    public const string SampleRecurringJobName = "SampleRecurringJob";
    public const string SampleOneTimeJobName = "SampleOneTimeJob";

    // ─── Trigger Groups ───────────────────────────────────────────────
    public const string RecurringTriggerGroup = "RecurringTriggers";
    public const string ManualTriggerGroup = "ManualTriggers";

    // ─── Trigger Names ────────────────────────────────────────────────
    public const string SampleRecurringTriggerName = "SampleRecurringTrigger";
    public const string SampleOneTimeTriggerName = "SampleOneTimeTrigger";

    // ─── Job Data Map Keys ────────────────────────────────────────────
    public const string RetryCountKey = "RetryCount";
    public const string MaxRetriesKey = "MaxRetries";
    public const string LastRunStatusKey = "LastRunStatus";
    public const string LastRunTimeKey = "LastRunTime";

    // ─── Observability ────────────────────────────────────────────────
    public const string MeterName = "Batch.Scheduler";
    public const string DynatraceServiceName = "BatchScheduler";
    public const string DynatraceServiceEndpoint = "quartz://batch-scheduler";
}
