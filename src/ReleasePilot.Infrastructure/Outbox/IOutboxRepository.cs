namespace ReleasePilot.Api.Infrastructure.Outbox;

public interface IOutboxRepository
{
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OutboxMessage>> GetPendingBatchAsync(int batchSize, int maxRetryCount, CancellationToken cancellationToken);

    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken);

    #pragma warning disable CA1716 // Identifiers should not match keywords
    Task MarkFailedAsync(Guid id, string error, int retryDelaySeconds, CancellationToken cancellationToken);
}
