using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record StartDeploymentCommand(Guid PromotionId, string ActingUser) : ICommand<PromotionDto>;

public sealed class StartDeploymentCommandHandler : ICommandHandler<StartDeploymentCommand, PromotionDto>
{
    private readonly IPromotionRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IDeploymentPort _deploymentPort;

    public StartDeploymentCommandHandler(
        IPromotionRepository repository,
        IDomainEventDispatcher eventDispatcher,
        IDeploymentPort deploymentPort)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _deploymentPort = deploymentPort;
    }

    public async Task<PromotionDto> HandleAsync(StartDeploymentCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion '{command.PromotionId}' was not found.");

        var existingPromotions = await _repository.ListAsync(cancellationToken);
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

        await _repository.UpdateAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return promotion.ToDto();
    }
}
