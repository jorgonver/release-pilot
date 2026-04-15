namespace ReleasePilot.Api.Application.Promotions.Events;

public sealed record PromotionEventMessage(
    Guid EventId,
    string CorrelationId,
    string EventType,
    Guid PromotionId,
    DateTimeOffset OccurredAt,
    string ActingUser,
    string PayloadJson);
