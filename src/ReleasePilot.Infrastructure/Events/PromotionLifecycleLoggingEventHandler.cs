using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Infrastructure.Logging;

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
        InfrastructureLogMessages.PromotionRequested(
            _logger,
            domainEvent.PromotionId,
            domainEvent.ApplicationName,
            domainEvent.Version,
            domainEvent.SourceEnvironment,
            domainEvent.TargetEnvironment);

        return Task.CompletedTask;
    }

    public Task HandleAsync(PromotionApprovedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        InfrastructureLogMessages.PromotionApproved(_logger, domainEvent.PromotionId);

        return Task.CompletedTask;
    }

    public Task HandleAsync(DeploymentStartedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        InfrastructureLogMessages.DeploymentStarted(_logger, domainEvent.PromotionId);

        return Task.CompletedTask;
    }

    public Task HandleAsync(PromotionCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        InfrastructureLogMessages.PromotionCompleted(_logger, domainEvent.PromotionId);

        return Task.CompletedTask;
    }

    public Task HandleAsync(PromotionRolledBackDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        InfrastructureLogMessages.PromotionRolledBack(_logger, domainEvent.PromotionId, domainEvent.Reason);

        return Task.CompletedTask;
    }

    public Task HandleAsync(PromotionCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        InfrastructureLogMessages.PromotionCancelled(_logger, domainEvent.PromotionId);

        return Task.CompletedTask;
    }
}
