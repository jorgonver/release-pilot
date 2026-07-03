using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Infrastructure.Logging;

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
        InfrastructureLogMessages.NotificationStub(
            _logger,
            notification.PromotionId,
            notification.TerminalState,
            notification.Reason ?? "n/a");

        return Task.CompletedTask;
    }
}
