using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Domain.Promotions.Events;

public sealed record PromotionRolledBackDomainEvent(Guid PromotionId, string Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
