namespace ReleasePilot.Api.Infrastructure.Outbox;

internal sealed class OutboxMessageRow
{
    public Guid Id { get; init; }

    public string EventType { get; init; } = string.Empty;

    public Guid AggregateId { get; init; }

    public DateTime OccurredAt { get; init; }

    public string PayloadJson { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTime? ProcessedAt { get; init; }

    public DateTime? NextAttemptAt { get; init; }

    public string? LastError { get; init; }

    public OutboxMessage ToOutboxMessage()
    {
        return new OutboxMessage(
            Id,
            EventType,
            AggregateId,
            ToUtcOffset(OccurredAt),
            PayloadJson,
            AttemptCount,
            ProcessedAt.HasValue ? ToUtcOffset(ProcessedAt.Value) : null,
            NextAttemptAt.HasValue ? ToUtcOffset(NextAttemptAt.Value) : null,
            LastError);
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        return new DateTimeOffset(utc);
    }
}
