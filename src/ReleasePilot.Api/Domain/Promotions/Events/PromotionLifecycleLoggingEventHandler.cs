using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Domain.Promotions.Events;

public sealed class PromotionLifecycleLoggingEventHandler :
    IDomainEventHandler<PromotionRequestedDomainEvent>,
    IDomainEventHandler<PromotionApprovedDomainEvent>,
    IDomainEventHandler<DeploymentStartedDomainEvent>,
    IDomainEventHandler<PromotionCompletedDomainEvent>,
    IDomainEventHandler<PromotionRolledBackDomainEvent>,
    IDomainEventHandler<PromotionCancelledDomainEvent>
{
    private readonly ILogger<PromotionLifecycleLoggingEventHandler> _logger;

    public PromotionLifecycleLoggingEventHandler(ILogger<PromotionLifecycleLoggingEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PromotionRequestedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PromotionRequested: {PromotionId} {ApplicationName} {Version} ({Source}->{Target})",
            domainEvent.PromotionId,
            domainEvent.ApplicationName,
            domainEvent.Version,
            domainEvent.SourceEnvironment,
            domainEvent.TargetEnvironment);

        return Task.CompletedTask;
    }

    public Task HandleAsync(PromotionApprovedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PromotionApproved: {PromotionId}", domainEvent.PromotionId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(DeploymentStartedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeploymentStarted: {PromotionId}", domainEvent.PromotionId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(PromotionCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PromotionCompleted: {PromotionId}", domainEvent.PromotionId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(PromotionRolledBackDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PromotionRolledBack: {PromotionId}, Reason: {Reason}", domainEvent.PromotionId, domainEvent.Reason);
        return Task.CompletedTask;
    }

    public Task HandleAsync(PromotionCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PromotionCancelled: {PromotionId}", domainEvent.PromotionId);
        return Task.CompletedTask;
    }
}
