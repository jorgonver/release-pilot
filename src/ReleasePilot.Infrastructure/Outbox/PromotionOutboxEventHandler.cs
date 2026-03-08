using System.Text.Json;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions.Events;
using ReleasePilot.Api.Domain.Promotions.Events;

namespace ReleasePilot.Api.Infrastructure.Outbox;

public sealed class PromotionOutboxEventHandler :
    IDomainEventHandler<PromotionRequestedDomainEvent>,
    IDomainEventHandler<PromotionApprovedDomainEvent>,
    IDomainEventHandler<DeploymentStartedDomainEvent>,
    IDomainEventHandler<PromotionCompletedDomainEvent>,
    IDomainEventHandler<PromotionRolledBackDomainEvent>,
    IDomainEventHandler<PromotionCancelledDomainEvent>
{
    private readonly IOutboxRepository _outboxRepository;

    public PromotionOutboxEventHandler(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
    }

    public Task HandleAsync(PromotionRequestedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return EnqueueAsync(domainEvent, "requested", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(PromotionApprovedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return EnqueueAsync(domainEvent, "approved", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(DeploymentStartedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return EnqueueAsync(domainEvent, "deployment_started", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(PromotionCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return EnqueueAsync(domainEvent, "completed", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(PromotionRolledBackDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return EnqueueAsync(domainEvent, "rolled_back", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(PromotionCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return EnqueueAsync(domainEvent, "cancelled", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    private Task EnqueueAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        string eventType,
        Guid promotionId,
        string actingUser,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var message = new PromotionEventMessage(
            Guid.NewGuid(),
            eventType,
            promotionId,
            occurredAt,
            actingUser,
            JsonSerializer.Serialize(domainEvent));

        var outboxMessage = new OutboxMessage(
            message.EventId,
            message.EventType,
            message.PromotionId,
            message.OccurredAt,
            JsonSerializer.Serialize(message),
            AttemptCount: 0,
            ProcessedAt: null,
            NextAttemptAt: null,
            LastError: null);

        return _outboxRepository.EnqueueAsync(outboxMessage, cancellationToken);
    }
}
