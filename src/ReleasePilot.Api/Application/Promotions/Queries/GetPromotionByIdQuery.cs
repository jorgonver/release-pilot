using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Application.Promotions.Queries;

public sealed record GetPromotionByIdQuery(Guid PromotionId) : IQuery<PromotionDto?>;

public sealed class GetPromotionByIdQueryHandler : IQueryHandler<GetPromotionByIdQuery, PromotionDto?>
{
    private readonly IPromotionRepository _repository;
    private readonly IIssueTrackerPort _issueTrackerPort;

    public GetPromotionByIdQueryHandler(IPromotionRepository repository, IIssueTrackerPort issueTrackerPort)
    {
        _repository = repository;
        _issueTrackerPort = issueTrackerPort;
    }

    public async Task<PromotionDto?> HandleAsync(GetPromotionByIdQuery query, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(query.PromotionId, cancellationToken);
        if (promotion is null)
        {
            return null;
        }

        var baseDto = promotion.ToDto();
        var references = promotion.WorkItems.Select(item => item.ExternalId).ToArray();
        var issueDetails = await _issueTrackerPort.GetWorkItemsAsync(references, cancellationToken);
        var issueById = issueDetails.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);

        var enrichedWorkItems = baseDto.WorkItems
            .Select(item => issueById.TryGetValue(item.ExternalId, out var detail)
                ? item with { Title = detail.Title, Description = detail.Description, Status = detail.Status }
                : item)
            .ToArray();

        return baseDto with { WorkItems = enrichedWorkItems };
    }
}
