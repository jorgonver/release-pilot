namespace ReleasePilot.Api.Domain.Promotions;

public enum PromotionStatus
{
    Requested = 0,
    Approved = 1,
    InProgress = 2,
    Completed = 3,
    RolledBack = 4,
    Cancelled = 5
}
