namespace ReleasePilot.Api.Application.Abstractions;

public interface INotificationPort
{
    Task NotifyPromotionTerminalStateAsync(
        PromotionTerminalStateNotification notification,
        CancellationToken cancellationToken);
}

public sealed record PromotionTerminalStateNotification(
    Guid PromotionId,
    string TerminalState,
    string? Reason,
    DateTimeOffset OccurredAt);
