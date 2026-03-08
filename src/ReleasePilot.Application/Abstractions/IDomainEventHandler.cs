using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Application.Abstractions;

public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
