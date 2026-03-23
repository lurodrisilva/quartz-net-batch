namespace Batch.Infrastructure.Configuration;

public sealed class QuartzSettings
{
    public const string SectionName = "Quartz";

    public string SchedulerName { get; set; } = "BatchScheduler";
    public string InstanceId { get; set; } = "AUTO";
    public int ThreadPoolSize { get; set; } = 10;
    public int MaxBatchSize { get; set; } = 5;
    public bool Clustered { get; set; } = true;
    public int ClusterCheckinIntervalMs { get; set; } = 15_000;
    public int MisfireThresholdMs { get; set; } = 60_000;
    public string TablePrefix { get; set; } = "QRTZ_";
    public bool PerformSchemaValidation { get; set; } = true;
}
