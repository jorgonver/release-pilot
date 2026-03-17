using ReleasePilot.Api.Domain.Primitives;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Domain.Tests;

public class EnvironmentPromotionPolicyTests
{
    [Fact]
    public void NormalizeDevelopmentReturnsDev()
    {
        var normalized = EnvironmentPromotionPolicy.Normalize(" development ");

        Assert.Equal("dev", normalized);
    }

    [Fact]
    public void EnsureKnownWithUnknownEnvironmentThrowsDomainRuleViolation()
    {
        var action = () => EnvironmentPromotionPolicy.EnsureKnown("qa", "targetEnvironment");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Unknown environment 'qa' in targetEnvironment. Allowed: dev, staging, production.", exception.Message);
    }

    [Fact]
    public void EnsureAdjacentPromotionPathWithValidPathsDoesNotThrow()
    {
        EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath("dev", "staging");
        EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath("staging", "production");
    }

    [Fact]
    public void EnsureAdjacentPromotionPathWithReverseOrSkippedPathThrowsDomainRuleViolation()
    {
        var reverse = () => EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath("production", "staging");
        var skipped = () => EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath("dev", "production");

        var reverseException = Assert.Throws<DomainRuleViolationException>(reverse);
        var skippedException = Assert.Throws<DomainRuleViolationException>(skipped);

        Assert.Equal("Environment promotion must follow fixed order without skipping: dev -> staging -> production.", reverseException.Message);
        Assert.Equal("Environment promotion must follow fixed order without skipping: dev -> staging -> production.", skippedException.Message);
    }
}
