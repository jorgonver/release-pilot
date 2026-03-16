using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record ApprovePromotionCommand(Guid PromotionId, string RequestedByRole, string ActingUser) : ICommand<PromotionCommandResult>;

public sealed class ApprovePromotionCommandHandler : ICommandHandler<ApprovePromotionCommand, PromotionCommandResult>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ApprovePromotionCommandHandler(IPromotionRepository promotionRepository, IDomainEventDispatcher eventDispatcher)
    {
        _promotionRepository = promotionRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PromotionCommandResult> HandleAsync(ApprovePromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion '{command.PromotionId}' was not found.");

        var existingPromotions = await _promotionRepository.ListAsync(cancellationToken);
        PromotionDomainRules.EnsureEnvironmentNotLocked(promotion, existingPromotions);

        promotion.Approve(command.RequestedByRole, command.ActingUser);

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return new PromotionCommandResult(promotion.Id);
    }
}
