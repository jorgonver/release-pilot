using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Application.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
