using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record ApprovePromotionCommand(Guid PromotionId, string RequestedByRole, string ActingUser) : ICommand<PromotionDto>;

public sealed class ApprovePromotionCommandHandler : ICommandHandler<ApprovePromotionCommand, PromotionDto>
{
    private readonly IPromotionRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ApprovePromotionCommandHandler(IPromotionRepository repository, IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PromotionDto> HandleAsync(ApprovePromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion '{command.PromotionId}' was not found.");

        var existingPromotions = await _repository.ListAsync(cancellationToken);
        PromotionDomainRules.EnsureEnvironmentNotLocked(promotion, existingPromotions);

        promotion.Approve(command.RequestedByRole, command.ActingUser);

        await _repository.UpdateAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return promotion.ToDto();
    }
}
