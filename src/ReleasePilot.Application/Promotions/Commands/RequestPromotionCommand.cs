using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record PromotionCommandResult(Guid Id);

public sealed record RequestPromotionCommand(
    string ApplicationName,
    string Version,
    string SourceEnvironment,
    string TargetEnvironment,
    string ActingUser,
    IReadOnlyCollection<RequestPromotionWorkItemInput> WorkItems) : ICommand<PromotionCommandResult>;

public sealed record RequestPromotionWorkItemInput(string ExternalId, string? Title);

public sealed class RequestPromotionCommandHandler : ICommandHandler<RequestPromotionCommand, PromotionCommandResult>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RequestPromotionCommandHandler(IPromotionRepository promotionRepository, IDomainEventDispatcher eventDispatcher)
    {
        _promotionRepository = promotionRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PromotionCommandResult> HandleAsync(RequestPromotionCommand command, CancellationToken cancellationToken)
    {
        var existingPromotions = await _promotionRepository.ListAsync(cancellationToken);
        PromotionDomainRules.EnsureCanRequest(
            command.ApplicationName,
            command.Version,
            command.SourceEnvironment,
            command.TargetEnvironment,
            existingPromotions);

        var workItems = command.WorkItems
            .Select(item => new WorkItemReference(item.ExternalId, item.Title))
            .ToArray();

        var promotion = Promotion.Create(
            command.ApplicationName,
            command.Version,
            command.SourceEnvironment,
            command.TargetEnvironment,
            command.ActingUser,
            workItems);

        await _promotionRepository.AddAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return new PromotionCommandResult(promotion.Id);
    }
}
