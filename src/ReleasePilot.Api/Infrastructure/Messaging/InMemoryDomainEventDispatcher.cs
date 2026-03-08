using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Infrastructure.Messaging;

public sealed class InMemoryDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryDomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);
            var handlers = (IEnumerable<object>)(_serviceProvider.GetService(enumerableType) ?? Array.Empty<object>());

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod is null)
                {
                    continue;
                }

                var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
                if (task is not null)
                {
                    tasks.Add(task);
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
    }
}
