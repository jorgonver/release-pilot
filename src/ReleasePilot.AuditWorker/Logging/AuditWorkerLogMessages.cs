namespace ReleasePilot.AuditWorker.Logging;

internal static partial class AuditWorkerLogMessages
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Connected to RabbitMQ and consuming queue '{QueueName}'.")]
    public static partial void ConnectedToRabbitMq(ILogger logger, string queueName);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Processing failed for event. Retrying attempt {Attempt}/{MaxAttempts}.")]
    public static partial void RetryingMessage(ILogger logger, int attempt, int maxAttempts);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Error,
        Message = "Processing failed after {MaxAttempts} attempts. Message moved to dead-letter queue.")]
    public static partial void MessageMovedToDeadLetter(ILogger logger, int maxAttempts);
}