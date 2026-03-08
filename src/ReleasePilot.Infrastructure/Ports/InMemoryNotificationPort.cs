using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Infrastructure.Ports;

public sealed class InMemoryNotificationPort : INotificationPort
{
    private readonly ILogger<InMemoryNotificationPort> _logger;

    public InMemoryNotificationPort(ILogger<InMemoryNotificationPort> logger)
    {
        _logger = logger;
    }

    public Task NotifyPromotionTerminalStateAsync(
        PromotionTerminalStateNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Notification stub: Promotion {PromotionId} reached terminal state {TerminalState}. Reason: {Reason}",
            notification.PromotionId,
            notification.TerminalState,
            notification.Reason ?? "n/a");

        return Task.CompletedTask;
    }
}
