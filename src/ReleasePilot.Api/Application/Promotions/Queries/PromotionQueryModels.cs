namespace ReleasePilot.Api.Application.Promotions.Queries;

public sealed record PaginatedPromotionsResult(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<PromotionDto> Items);

public sealed record EnvironmentStatusResult(
    string ApplicationName,
    IReadOnlyCollection<EnvironmentPromotionStatusItem> Environments);

public sealed record EnvironmentPromotionStatusItem(
    string Environment,
    Guid? PromotionId,
    string? Version,
    string CurrentState,
    DateTimeOffset? UpdatedAt);
