namespace Batch.Infrastructure.Configuration;

public sealed class JobScheduleSettings
{
    public const string SectionName = "JobSchedules";

    public RecurringJobSchedule RecurringJob { get; set; } = new();
    public OneTimeJobSchedule OneTimeJob { get; set; } = new();
}

public sealed class RecurringJobSchedule
{
    public bool Enabled { get; set; } = true;
    public string CronExpression { get; set; } = "0 0/5 * * * ?";
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 3;
}

public sealed class OneTimeJobSchedule
{
    public bool Enabled { get; set; } = true;
    public int DelaySeconds { get; set; } = 30;
}
