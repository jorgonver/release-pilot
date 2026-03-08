namespace ReleasePilot.Api.Infrastructure.Outbox;

public sealed record OutboxMessage(
    Guid Id,
    string EventType,
    Guid AggregateId,
    DateTimeOffset OccurredAt,
    string PayloadJson,
    int AttemptCount,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? NextAttemptAt,
    string? LastError);
