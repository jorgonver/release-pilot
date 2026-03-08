using ReleasePilot.Api.Application.Promotions.Events;

namespace ReleasePilot.AuditWorker;

public sealed class AuditLogConsumerWorker : BackgroundService
{
    private readonly ILogger<AuditLogConsumerWorker> _logger;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPromotionEventConsumer _promotionEventConsumer;

    public AuditLogConsumerWorker(
        IAuditLogRepository auditLogRepository,
        IPromotionEventConsumer promotionEventConsumer,
        ILogger<AuditLogConsumerWorker> logger)
    {
        _auditLogRepository = auditLogRepository;
        _promotionEventConsumer = promotionEventConsumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _auditLogRepository.EnsurePrerequisitesAsync(stoppingToken);
        await _promotionEventConsumer.StartAsync(HandleMessageAsync, stoppingToken);
        _logger.LogInformation("Audit worker started and is consuming promotion events.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private Task HandleMessageAsync(PromotionEventMessage message, CancellationToken cancellationToken)
    {
        return _auditLogRepository.InsertAsync(message, cancellationToken);
    }
}
