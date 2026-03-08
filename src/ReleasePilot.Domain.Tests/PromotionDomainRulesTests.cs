using ReleasePilot.Api.Domain.Primitives;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Domain.Tests;

public class PromotionDomainRulesTests
{
    [Fact]
    public void EnsureCanRequest_ToProduction_WhenCompletedInStaging_DoesNotThrow()
    {
        var completedStaging = Promotion.Rehydrate(
            id: Guid.NewGuid(),
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            status: PromotionStatus.Completed,
            createdAt: DateTimeOffset.UtcNow.AddHours(-2),
            updatedAt: DateTimeOffset.UtcNow.AddHours(-1),
            rolledBackReason: null,
            completedAt: DateTimeOffset.UtcNow.AddHours(-1),
            workItems: [],
            stateHistory: []);

        PromotionDomainRules.EnsureCanRequest(
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "staging",
            targetEnvironment: "production",
            existingPromotions: [completedStaging]);
    }

    [Fact]
    public void EnsureCanRequest_ToProduction_WithoutCompletedInStaging_ThrowsDomainRuleViolation()
    {
        var pendingStaging = Promotion.Rehydrate(
            id: Guid.NewGuid(),
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            status: PromotionStatus.InProgress,
            createdAt: DateTimeOffset.UtcNow.AddHours(-2),
            updatedAt: DateTimeOffset.UtcNow.AddHours(-1),
            rolledBackReason: null,
            completedAt: null,
            workItems: [],
            stateHistory: []);

        var action = () => PromotionDomainRules.EnsureCanRequest(
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "staging",
            targetEnvironment: "production",
            existingPromotions: [pendingStaging]);

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Cannot request promotion to production before this version is completed in staging.", exception.Message);
    }

    [Fact]
    public void EnsureEnvironmentNotLocked_WhenOtherInProgressForSameAppAndTarget_ThrowsDomainRuleViolation()
    {
        var current = Promotion.Rehydrate(
            id: Guid.NewGuid(),
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            status: PromotionStatus.Approved,
            createdAt: DateTimeOffset.UtcNow.AddHours(-1),
            updatedAt: DateTimeOffset.UtcNow,
            rolledBackReason: null,
            completedAt: null,
            workItems: [],
            stateHistory: []);

        var otherInProgress = Promotion.Rehydrate(
            id: Guid.NewGuid(),
            applicationName: "checkout-service",
            version: "2.0.0",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            status: PromotionStatus.InProgress,
            createdAt: DateTimeOffset.UtcNow.AddHours(-2),
            updatedAt: DateTimeOffset.UtcNow.AddHours(-1),
            rolledBackReason: null,
            completedAt: null,
            workItems: [],
            stateHistory: []);

        var action = () => PromotionDomainRules.EnsureEnvironmentNotLocked(current, [otherInProgress]);

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Another promotion for application 'checkout-service' targeting 'staging' is already in progress.", exception.Message);
    }

    [Fact]
    public void EnsureEnvironmentNotLocked_WhenInProgressIsDifferentTarget_DoesNotThrow()
    {
        var current = Promotion.Rehydrate(
            id: Guid.NewGuid(),
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            status: PromotionStatus.Approved,
            createdAt: DateTimeOffset.UtcNow.AddHours(-1),
            updatedAt: DateTimeOffset.UtcNow,
            rolledBackReason: null,
            completedAt: null,
            workItems: [],
            stateHistory: []);

        var otherInProgressDifferentTarget = Promotion.Rehydrate(
            id: Guid.NewGuid(),
            applicationName: "checkout-service",
            version: "2.0.0",
            sourceEnvironment: "staging",
            targetEnvironment: "production",
            status: PromotionStatus.InProgress,
            createdAt: DateTimeOffset.UtcNow.AddHours(-2),
            updatedAt: DateTimeOffset.UtcNow.AddHours(-1),
            rolledBackReason: null,
            completedAt: null,
            workItems: [],
            stateHistory: []);

        PromotionDomainRules.EnsureEnvironmentNotLocked(current, [otherInProgressDifferentTarget]);
    }
}
