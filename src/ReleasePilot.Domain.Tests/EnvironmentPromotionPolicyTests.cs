using ReleasePilot.Api.Domain.Primitives;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Domain.Tests;

public class EnvironmentPromotionPolicyTests
{
    [Fact]
    public void Normalize_Development_ReturnsDev()
    {
        var normalized = EnvironmentPromotionPolicy.Normalize(" development ");

        Assert.Equal("dev", normalized);
    }

    [Fact]
    public void EnsureKnown_WithUnknownEnvironment_ThrowsDomainRuleViolation()
    {
        var action = () => EnvironmentPromotionPolicy.EnsureKnown("qa", "targetEnvironment");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Unknown environment 'qa' in targetEnvironment. Allowed: dev, staging, production.", exception.Message);
    }

    [Fact]
    public void EnsureAdjacentPromotionPath_WithValidPaths_DoesNotThrow()
    {
        EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath("dev", "staging");
        EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath("staging", "production");
    }

    [Fact]
    public void EnsureAdjacentPromotionPath_WithReverseOrSkippedPath_ThrowsDomainRuleViolation()
    {
        var reverse = () => EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath("production", "staging");
        var skipped = () => EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath("dev", "production");

        var reverseException = Assert.Throws<DomainRuleViolationException>(reverse);
        var skippedException = Assert.Throws<DomainRuleViolationException>(skipped);

        Assert.Equal("Environment promotion must follow fixed order without skipping: dev -> staging -> production.", reverseException.Message);
        Assert.Equal("Environment promotion must follow fixed order without skipping: dev -> staging -> production.", skippedException.Message);
    }
}
