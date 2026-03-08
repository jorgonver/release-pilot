using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions.Events;

namespace ReleasePilot.Api.Application.Promotions.Events;

public sealed class PromotionTerminalStateNotificationHandler :
    IDomainEventHandler<PromotionCompletedDomainEvent>,
    IDomainEventHandler<PromotionRolledBackDomainEvent>,
    IDomainEventHandler<PromotionCancelledDomainEvent>
{
    private readonly INotificationPort _notificationPort;

    public PromotionTerminalStateNotificationHandler(INotificationPort notificationPort)
    {
        _notificationPort = notificationPort;
    }

    public Task HandleAsync(PromotionCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return _notificationPort.NotifyPromotionTerminalStateAsync(
            new PromotionTerminalStateNotification(
                domainEvent.PromotionId,
                "Completed",
                null,
                domainEvent.OccurredAt),
            cancellationToken);
    }

    public Task HandleAsync(PromotionRolledBackDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return _notificationPort.NotifyPromotionTerminalStateAsync(
            new PromotionTerminalStateNotification(
                domainEvent.PromotionId,
                "RolledBack",
                domainEvent.Reason,
                domainEvent.OccurredAt),
            cancellationToken);
    }

    public Task HandleAsync(PromotionCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return _notificationPort.NotifyPromotionTerminalStateAsync(
            new PromotionTerminalStateNotification(
                domainEvent.PromotionId,
                "Cancelled",
                null,
                domainEvent.OccurredAt),
            cancellationToken);
    }
}
