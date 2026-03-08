using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record RollbackPromotionCommand(Guid PromotionId, string Reason, string ActingUser) : ICommand<PromotionDto>;

public sealed class RollbackPromotionCommandHandler : ICommandHandler<RollbackPromotionCommand, PromotionDto>
{
    private readonly IPromotionRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RollbackPromotionCommandHandler(IPromotionRepository repository, IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PromotionDto> HandleAsync(RollbackPromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion '{command.PromotionId}' was not found.");

        promotion.Rollback(command.Reason, command.ActingUser);

        await _repository.UpdateAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return promotion.ToDto();
    }
}
