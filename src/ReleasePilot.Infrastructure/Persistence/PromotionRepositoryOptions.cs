namespace ReleasePilot.Api.Infrastructure.Persistence;

public sealed class PromotionRepositoryOptions
{
    public const string SectionName = "PromotionRepository";

    public string ConnectionString { get; set; } = string.Empty;
}
