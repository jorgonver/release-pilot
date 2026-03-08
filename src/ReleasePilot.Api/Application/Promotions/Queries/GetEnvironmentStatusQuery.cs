using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Application.Promotions.Queries;

public sealed record GetEnvironmentStatusQuery(string ApplicationName) : IQuery<EnvironmentStatusResult>;

public sealed class GetEnvironmentStatusQueryHandler : IQueryHandler<GetEnvironmentStatusQuery, EnvironmentStatusResult>
{
    private static readonly string[] OrderedEnvironments = ["dev", "staging", "production"];

    private readonly IPromotionRepository _repository;

    public GetEnvironmentStatusQueryHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    public async Task<EnvironmentStatusResult> HandleAsync(GetEnvironmentStatusQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.ApplicationName))
        {
            throw new DomainRuleViolationException("Application name is required.");
        }

        var promotions = await _repository.ListByApplicationAsync(query.ApplicationName.Trim(), cancellationToken);
        var environmentItems = OrderedEnvironments
            .Select(environment => BuildEnvironmentStatusItem(environment, promotions))
            .ToArray();

        return new EnvironmentStatusResult(query.ApplicationName.Trim(), environmentItems);
    }

    private static EnvironmentPromotionStatusItem BuildEnvironmentStatusItem(
        string environment,
        IReadOnlyCollection<Domain.Promotions.Promotion> promotions)
    {
        var latest = promotions
            .Where(item => item.TargetEnvironment.Equals(environment, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            return new EnvironmentPromotionStatusItem(environment, null, null, "None", null);
        }

        return new EnvironmentPromotionStatusItem(
            environment,
            latest.Id,
            latest.Version,
            latest.Status.ToString(),
            latest.UpdatedAt);
    }
}
