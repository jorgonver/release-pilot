using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Application.Promotions;

public static class PromotionMappings
{
    public static PromotionDto ToDto(this Promotion promotion)
    {
        return new PromotionDto(
            promotion.Id,
            promotion.ApplicationName,
            promotion.Version,
            promotion.SourceEnvironment,
            promotion.TargetEnvironment,
            promotion.Status.ToString(),
            promotion.RolledBackReason,
            promotion.CompletedAt,
            promotion.CreatedAt,
            promotion.UpdatedAt,
            promotion.StateHistory.Select(entry => new PromotionStateHistoryEntryDto(
                entry.FromState?.ToString(),
                entry.ToState.ToString(),
                entry.Command,
                entry.OccurredAt,
                entry.Notes)).ToArray(),
            promotion.WorkItems.Select(item => new WorkItemReferenceDto(item.ExternalId, item.Title, null, null)).ToArray());
    }
}
