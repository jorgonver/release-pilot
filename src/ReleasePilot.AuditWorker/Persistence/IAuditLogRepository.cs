using ReleasePilot.Api.Application.Promotions.Events;

namespace ReleasePilot.AuditWorker;

public interface IAuditLogRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken);

    Task InsertAsync(PromotionEventMessage message, CancellationToken cancellationToken);
}
