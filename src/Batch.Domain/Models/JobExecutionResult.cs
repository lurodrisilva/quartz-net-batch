namespace Batch.Domain.Models;

/// <summary>
/// Represents the outcome of a job execution.
/// </summary>
public sealed record JobExecutionResult
{
    public required string JobName { get; init; }
    public required string JobGroup { get; init; }
    public required string FireInstanceId { get; init; }
    public required DateTimeOffset ScheduledFireTime { get; init; }
    public required DateTimeOffset ActualFireTime { get; init; }
    public required TimeSpan Duration { get; init; }
    public required JobOutcome Outcome { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ExceptionType { get; init; }
    public int ItemsProcessed { get; init; }
}

/// <summary>
/// Possible outcomes for a job execution.
/// </summary>
public enum JobOutcome
{
    Success,
    Failed,
    Cancelled,
    Skipped
}
