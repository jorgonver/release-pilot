using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Application.Promotions.Queries;

public sealed record ListPromotionsQuery : IQuery<IReadOnlyCollection<PromotionDto>>;

public sealed class ListPromotionsQueryHandler : IQueryHandler<ListPromotionsQuery, IReadOnlyCollection<PromotionDto>>
{
    private readonly IPromotionRepository _repository;

    public ListPromotionsQueryHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<PromotionDto>> HandleAsync(ListPromotionsQuery query, CancellationToken cancellationToken)
    {
        var promotions = await _repository.ListAsync(cancellationToken);
        return promotions
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => item.ToDto())
            .ToArray();
    }
}

public sealed record ListApplicationsQuery : IQuery<IReadOnlyCollection<string>>;

public sealed class ListApplicationsQueryHandler : IQueryHandler<ListApplicationsQuery, IReadOnlyCollection<string>>
{
    private readonly IPromotionRepository _repository;

    public ListApplicationsQueryHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<string>> HandleAsync(ListApplicationsQuery query, CancellationToken cancellationToken)
    {
        var promotions = await _repository.ListAsync(cancellationToken);

        return promotions
            .Select(item => item.ApplicationName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
