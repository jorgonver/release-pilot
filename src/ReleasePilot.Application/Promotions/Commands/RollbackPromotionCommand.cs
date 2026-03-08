using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record RollbackPromotionCommand(Guid PromotionId, string Reason, string ActingUser) : ICommand<PromotionDto>;

public sealed class RollbackPromotionCommandHandler : ICommandHandler<RollbackPromotionCommand, PromotionDto>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RollbackPromotionCommandHandler(IPromotionRepository promotionRepository, IDomainEventDispatcher eventDispatcher)
    {
        _promotionRepository = promotionRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PromotionDto> HandleAsync(RollbackPromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion '{command.PromotionId}' was not found.");

        promotion.Rollback(command.Reason, command.ActingUser);

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return promotion.ToDto();
    }
}
