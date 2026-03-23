namespace Batch.Domain.Interfaces;

/// <summary>
/// Abstraction for tracing job execution boundaries.
/// Implementations may use Dynatrace OneAgent SDK, OpenTelemetry, or other providers.
/// </summary>
public interface IJobTracer
{
    /// <summary>
    /// Starts a trace span for a batch job execution.
    /// Returns a disposable scope that ends the trace when disposed.
    /// </summary>
    IJobTraceScope StartJobTrace(string jobName, string jobGroup, string fireInstanceId);
}

/// <summary>
/// Represents an active trace scope for a job execution.
/// Disposing ends the trace span.
/// </summary>
public interface IJobTraceScope : IDisposable
{
    /// <summary>
    /// Marks the trace with an error.
    /// </summary>
    void SetError(Exception exception);

    /// <summary>
    /// Adds a custom attribute to the trace.
    /// </summary>
    void SetAttribute(string key, string value);
}
