namespace ReleasePilot.Api.Domain.Primitives;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
