namespace ReleasePilot.Api.Domain.Promotions;

public sealed record PromotionStateHistoryEntry(
    PromotionStatus? FromState,
    PromotionStatus ToState,
    string Command,
    DateTimeOffset OccurredAt,
    string? Notes = null);
