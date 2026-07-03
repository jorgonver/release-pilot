namespace ReleasePilot.AuditWorker.Logging;

internal static partial class AuditWorkerLogMessages
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Connected to RabbitMQ and consuming queue '{QueueName}'.")]
    public static partial void ConnectedToRabbitMq(ILogger logger, string queueName);
}