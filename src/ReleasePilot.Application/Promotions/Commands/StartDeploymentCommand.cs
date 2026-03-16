using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record StartDeploymentCommand(Guid PromotionId, string ActingUser) : ICommand<PromotionCommandResult>;

public sealed class StartDeploymentCommandHandler : ICommandHandler<StartDeploymentCommand, PromotionCommandResult>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IDeploymentPort _deploymentPort;

    public StartDeploymentCommandHandler(
        IPromotionRepository promotionRepository,
        IDomainEventDispatcher eventDispatcher,
        IDeploymentPort deploymentPort)
    {
        _promotionRepository = promotionRepository;
        _eventDispatcher = eventDispatcher;
        _deploymentPort = deploymentPort;
    }

    public async Task<PromotionCommandResult> HandleAsync(StartDeploymentCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion '{command.PromotionId}' was not found.");

        var existingPromotions = await _promotionRepository.ListAsync(cancellationToken);
        PromotionDomainRules.EnsureEnvironmentNotLocked(promotion, existingPromotions);

        await _deploymentPort.StartDeploymentAsync(
            new DeploymentRequest(
                promotion.Id,
                promotion.ApplicationName,
                promotion.Version,
                promotion.SourceEnvironment,
                promotion.TargetEnvironment),
            cancellationToken);

        promotion.Start(command.ActingUser);

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return new PromotionCommandResult(promotion.Id);
    }
}
