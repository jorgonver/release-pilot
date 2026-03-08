using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record CompletePromotionCommand(Guid PromotionId) : ICommand<PromotionDto>;

public sealed class CompletePromotionCommandHandler : ICommandHandler<CompletePromotionCommand, PromotionDto>
{
    private readonly IPromotionRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CompletePromotionCommandHandler(IPromotionRepository repository, IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PromotionDto> HandleAsync(CompletePromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion '{command.PromotionId}' was not found.");

        promotion.Complete();

        await _repository.UpdateAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return promotion.ToDto();
    }
}
