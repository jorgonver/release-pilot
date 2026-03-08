using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Application.Promotions.Commands;

public sealed record RequestPromotionCommand(
    string ApplicationName,
    string Version,
    string SourceEnvironment,
    string TargetEnvironment,
    string ActingUser,
    IReadOnlyCollection<RequestPromotionWorkItemInput> WorkItems) : ICommand<PromotionDto>;

public sealed record RequestPromotionWorkItemInput(string ExternalId, string? Title);

public sealed class RequestPromotionCommandHandler : ICommandHandler<RequestPromotionCommand, PromotionDto>
{
    private readonly IPromotionRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RequestPromotionCommandHandler(IPromotionRepository repository, IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PromotionDto> HandleAsync(RequestPromotionCommand command, CancellationToken cancellationToken)
    {
        var existingPromotions = await _repository.ListAsync(cancellationToken);
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

        await _repository.AddAsync(promotion, cancellationToken);
        await _eventDispatcher.DispatchAsync(promotion.PullDomainEvents(), cancellationToken);

        return promotion.ToDto();
    }
}
