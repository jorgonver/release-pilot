using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Domain.Promotions;

public static class EnvironmentPromotionPolicy
{
    private static readonly string[] Ordered = ["dev", "staging", "production"];

    public static string Normalize(string environment)
    {
        var normalized = environment.Trim().ToLowerInvariant();
        if (normalized == "development")
        {
            return "dev";
        }

        return normalized;
    }

    public static bool IsKnown(string environment)
    {
        var normalized = Normalize(environment);
        return Ordered.Contains(normalized, StringComparer.Ordinal);
    }

    public static void EnsureKnown(string environment, string fieldName)
    {
        if (!IsKnown(environment))
        {
            throw new DomainRuleViolationException($"Unknown environment '{environment}' in {fieldName}. Allowed: dev, staging, production.");
        }
    }

    public static void EnsureAdjacentPromotionPath(string sourceEnvironment, string targetEnvironment)
    {
        var source = Normalize(sourceEnvironment);
        var target = Normalize(targetEnvironment);

        var sourceIndex = Array.IndexOf(Ordered, source);
        var targetIndex = Array.IndexOf(Ordered, target);
        if (sourceIndex < 0 || targetIndex < 0)
        {
            throw new DomainRuleViolationException("Promotion path includes unknown environment.");
        }

        if (targetIndex - sourceIndex != 1)
        {
            throw new DomainRuleViolationException("Environment promotion must follow fixed order without skipping: dev -> staging -> production.");
        }
    }
}
