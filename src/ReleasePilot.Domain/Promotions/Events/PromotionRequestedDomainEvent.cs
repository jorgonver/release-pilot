using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Domain.Promotions.Events;

public sealed record PromotionRequestedDomainEvent(
    Guid PromotionId,
    string ApplicationName,
    string Version,
    string SourceEnvironment,
    string TargetEnvironment,
    string ActingUser) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
