using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Domain.Promotions.Events;

public sealed record PromotionApprovedDomainEvent(Guid PromotionId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
