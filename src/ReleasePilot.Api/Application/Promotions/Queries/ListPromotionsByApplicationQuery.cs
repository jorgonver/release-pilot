using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Application.Promotions.Queries;

public sealed record ListPromotionsByApplicationQuery(string ApplicationName, int Page, int PageSize)
    : IQuery<PaginatedPromotionsResult>;

public sealed class ListPromotionsByApplicationQueryHandler
    : IQueryHandler<ListPromotionsByApplicationQuery, PaginatedPromotionsResult>
{
    private readonly IPromotionRepository _repository;

    public ListPromotionsByApplicationQueryHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedPromotionsResult> HandleAsync(ListPromotionsByApplicationQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.ApplicationName))
        {
            throw new DomainRuleViolationException("Application name is required.");
        }

        if (query.Page <= 0)
        {
            throw new DomainRuleViolationException("Page must be greater than 0.");
        }

        if (query.PageSize <= 0 || query.PageSize > 200)
        {
            throw new DomainRuleViolationException("PageSize must be between 1 and 200.");
        }

        var promotions = await _repository.ListByApplicationAsync(query.ApplicationName.Trim(), cancellationToken);
        var ordered = promotions
            .OrderByDescending(item => item.CreatedAt)
            .ToArray();

        var totalCount = ordered.Length;
        var items = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => item.ToDto())
            .ToArray();

        return new PaginatedPromotionsResult(query.Page, query.PageSize, totalCount, items);
    }
}
