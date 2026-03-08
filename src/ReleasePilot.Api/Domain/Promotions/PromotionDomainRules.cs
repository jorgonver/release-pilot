using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Domain.Promotions;

public static class PromotionDomainRules
{
    public static void EnsureCanRequest(
        string applicationName,
        string version,
        string sourceEnvironment,
        string targetEnvironment,
        IReadOnlyCollection<Promotion> existingPromotions)
    {
        var normalizedApplication = applicationName.Trim();
        var normalizedVersion = version.Trim();
        var normalizedSource = EnvironmentPromotionPolicy.Normalize(sourceEnvironment);
        var normalizedTarget = EnvironmentPromotionPolicy.Normalize(targetEnvironment);

        EnvironmentPromotionPolicy.EnsureKnown(normalizedSource, nameof(sourceEnvironment));
        EnvironmentPromotionPolicy.EnsureKnown(normalizedTarget, nameof(targetEnvironment));
        EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath(normalizedSource, normalizedTarget);

        if (normalizedTarget == "production")
        {
            var reachedStaging = existingPromotions.Any(item =>
                item.ApplicationName.Equals(normalizedApplication, StringComparison.OrdinalIgnoreCase)
                && item.Version.Equals(normalizedVersion, StringComparison.OrdinalIgnoreCase)
                && item.TargetEnvironment == "staging"
                && item.Status == PromotionStatus.Completed);

            if (!reachedStaging)
            {
                throw new DomainRuleViolationException(
                    "Cannot request promotion to production before this version is completed in staging.");
            }
        }
    }

    public static void EnsureEnvironmentNotLocked(Promotion current, IReadOnlyCollection<Promotion> existingPromotions)
    {
        var hasOtherInProgress = existingPromotions.Any(item =>
            item.Id != current.Id
            && item.ApplicationName.Equals(current.ApplicationName, StringComparison.OrdinalIgnoreCase)
            && item.TargetEnvironment.Equals(current.TargetEnvironment, StringComparison.OrdinalIgnoreCase)
            && item.Status == PromotionStatus.InProgress);

        if (hasOtherInProgress)
        {
            throw new DomainRuleViolationException(
                $"Another promotion for application '{current.ApplicationName}' targeting '{current.TargetEnvironment}' is already in progress.");
        }
    }
}
