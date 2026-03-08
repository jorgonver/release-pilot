using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record CancelPromotionCommand(Guid PromotionId, string ActingUser) : ICommand<PromotionDto>;

public sealed class CancelPromotionCommandHandler : ICommandHandler<CancelPromotionCommand, PromotionDto>
{
    private readonly IPromotionRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CancelPromotionCommandHandler(IPromotionRepository repository, IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PromotionDto> HandleAsync(CancelPromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion '{command.PromotionId}' was not found.");

        promotion.Cancel(command.ActingUser);

        await _repository.UpdateAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return promotion.ToDto();
    }
}
