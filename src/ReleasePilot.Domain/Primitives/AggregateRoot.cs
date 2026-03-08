namespace ReleasePilot.Api.Domain.Primitives;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyCollection<IDomainEvent> PullDomainEvents()
    {
        var pending = _domainEvents.ToArray();
        _domainEvents.Clear();
        return pending;
    }
}
