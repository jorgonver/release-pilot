using ReleasePilot.Api.Application.Promotions.Events;

namespace ReleasePilot.AuditWorker;

public interface IPromotionEventConsumer : IAsyncDisposable
{
    Task StartAsync(
        Func<PromotionEventMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken);
}
