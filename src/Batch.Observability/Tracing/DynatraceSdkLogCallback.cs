using Dynatrace.OneAgent.Sdk.Api;
using Microsoft.Extensions.Logging;

namespace Batch.Observability.Tracing;

public sealed partial class DynatraceSdkLogCallback : ILoggingCallback
{
    private readonly ILogger _logger;

    public DynatraceSdkLogCallback(ILogger<DynatraceSdkLogCallback> logger)
    {
        _logger = logger;
    }

    public void Error(string message) => LogSdkError(_logger, message);

    public void Warn(string message) => LogSdkWarning(_logger, message);

    [LoggerMessage(Level = LogLevel.Error, Message = "[OneAgent SDK] {Message}")]
    private static partial void LogSdkError(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[OneAgent SDK] {Message}")]
    private static partial void LogSdkWarning(ILogger logger, string message);
}
