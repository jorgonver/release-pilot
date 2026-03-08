namespace ReleasePilot.Api.Application.Promotions;

public sealed record PromotionDto(
    Guid Id,
    string ApplicationName,
    string Version,
    string SourceEnvironment,
    string TargetEnvironment,
    string Status,
    string? RolledBackReason,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<PromotionStateHistoryEntryDto> StateHistory,
    IReadOnlyCollection<WorkItemReferenceDto> WorkItems);

public sealed record WorkItemReferenceDto(
    string ExternalId,
    string? Title,
    string? Description,
    string? Status);

public sealed record PromotionStateHistoryEntryDto(
    string? FromState,
    string ToState,
    string Command,
    DateTimeOffset OccurredAt,
    string? Notes);
