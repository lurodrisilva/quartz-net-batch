namespace Batch.Domain.Interfaces;

/// <summary>
/// Abstraction for recording job execution metrics.
/// Implementations may use System.Diagnostics.Metrics, Prometheus, or other providers.
/// </summary>
public interface IJobMetrics
{
    /// <summary>
    /// Records that a job execution has started.
    /// </summary>
    void RecordJobStarted(string jobName, string jobGroup);

    /// <summary>
    /// Records that a job execution completed successfully.
    /// </summary>
    void RecordJobSucceeded(string jobName, string jobGroup);

    /// <summary>
    /// Records that a job execution failed.
    /// </summary>
    void RecordJobFailed(string jobName, string jobGroup, string exceptionType);

    /// <summary>
    /// Records the duration of a job execution in milliseconds.
    /// </summary>
    void RecordJobDuration(string jobName, string jobGroup, double durationMs);

    /// <summary>
    /// Records a job retry attempt.
    /// </summary>
    void RecordJobRetry(string jobName, string jobGroup);

    /// <summary>
    /// Records a misfire event.
    /// </summary>
    void RecordMisfire(string jobName, string jobGroup);
}
