namespace ReleasePilot.Api.Dto;

public sealed record RequestPromotionDto(
    string ApplicationName,
    string Version,
    string SourceEnvironment,
    string TargetEnvironment,
    string ActingUser,
    IReadOnlyCollection<RequestPromotionWorkItemDto> WorkItems);

public sealed record RequestPromotionWorkItemDto(string ExternalId, string? Title);

public sealed record ApprovePromotionDto(string RequestedByRole, string ActingUser);

public sealed record RollbackPromotionDto(string Reason, string ActingUser);

public sealed record ActingUserDto(string ActingUser);
