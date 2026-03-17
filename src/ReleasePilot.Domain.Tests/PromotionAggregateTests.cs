using ReleasePilot.Api.Domain.Primitives;
using ReleasePilot.Api.Domain.Promotions;
using ReleasePilot.Api.Domain.Promotions.Events;

namespace ReleasePilot.Domain.Tests;

public class PromotionAggregateTests
{
    [Fact]
    public void CreateWithValidDataSetsRequestedStateAndRaisesRequestedEvent()
    {
        var promotion = Promotion.Create(
            applicationName: " checkout-service ",
            version: " 1.2.3 ",
            sourceEnvironment: "development",
            targetEnvironment: "staging",
            actingUser: "  alice ",
            workItems:
            [
                new WorkItemReference(" WI-123 ", " Fix checkout timeout ")
            ]);

        Assert.Equal(PromotionStatus.Requested, promotion.Status);
        Assert.Equal("checkout-service", promotion.ApplicationName);
        Assert.Equal("1.2.3", promotion.Version);
        Assert.Equal("dev", promotion.SourceEnvironment);
        Assert.Equal("staging", promotion.TargetEnvironment);

        var domainEvent = Assert.IsType<PromotionRequestedDomainEvent>(Assert.Single(promotion.DomainEvents));
        Assert.Equal(promotion.Id, domainEvent.PromotionId);
        Assert.Equal("alice", domainEvent.ActingUser);
    }

    [Fact]
    public void ApproveWithNonApproverRoleThrowsDomainRuleViolation()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();

        var action = () => promotion.Approve("Developer", "bob");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Only users with Approver role may approve a promotion.", exception.Message);
    }

    [Fact]
    public void ApproveWithApproverRoleTransitionsToApprovedAndRaisesEvent()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();

        promotion.Approve("Approver", " approver-user ");

        Assert.Equal(PromotionStatus.Approved, promotion.Status);
        var approvedEvent = Assert.IsType<PromotionApprovedDomainEvent>(Assert.Single(promotion.DomainEvents));
        Assert.Equal(promotion.Id, approvedEvent.PromotionId);
        Assert.Equal("approver-user", approvedEvent.ActingUser);
        Assert.Equal("ApprovePromotion", promotion.StateHistory.Last().Command);
    }

    [Fact]
    public void StartFromRequestedThrowsDomainRuleViolation()
    {
        var promotion = CreateRequestedPromotion();

        var action = () => promotion.Start("deployer-user");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Only approved promotions can be started.", exception.Message);
    }

    [Fact]
    public void CompleteFromApprovedThrowsDomainRuleViolation()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();
        promotion.Approve("Approver", "approver-user");

        var action = () => promotion.Complete("deployer-user");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Only in-progress promotions can be completed.", exception.Message);
    }

    [Fact]
    public void CompleteAfterApproveAndStartSetsCompletedAtAndEmitsCompletedEvent()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();

        promotion.Approve("Approver", "approver-user");
        promotion.Start("deployer-user");
        promotion.Complete("deployer-user");

        Assert.Equal(PromotionStatus.Completed, promotion.Status);
        Assert.NotNull(promotion.CompletedAt);

        var completedEvent = Assert.IsType<PromotionCompletedDomainEvent>(promotion.DomainEvents.Last());
        Assert.Equal(promotion.Id, completedEvent.PromotionId);
        Assert.Equal("deployer-user", completedEvent.ActingUser);
    }

    [Fact]
    public void RollbackWithoutReasonThrowsDomainRuleViolation()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();
        promotion.Approve("Approver", "approver-user");
        promotion.Start("deployer-user");

        var action = () => promotion.Rollback("   ", "deployer-user");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Rollback reason is required.", exception.Message);
    }

    [Fact]
    public void RollbackWithReasonTransitionsToRolledBackAndRaisesEvent()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();
        promotion.Approve("Approver", "approver-user");
        promotion.Start("deployer-user");
        promotion.PullDomainEvents();

        promotion.Rollback(" deployment failed ", "deployer-user");

        Assert.Equal(PromotionStatus.RolledBack, promotion.Status);
        Assert.Equal("deployment failed", promotion.RolledBackReason);
        var rollbackEvent = Assert.IsType<PromotionRolledBackDomainEvent>(Assert.Single(promotion.DomainEvents));
        Assert.Equal(promotion.Id, rollbackEvent.PromotionId);
        Assert.Equal("deployment failed", rollbackEvent.Reason);
        Assert.Equal("deployer-user", rollbackEvent.ActingUser);
    }

    [Fact]
    public void CancelAfterApprovalThrowsDomainRuleViolation()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();
        promotion.Approve("Approver", "approver-user");

        var action = () => promotion.Cancel("requester-user");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Only requested promotions can be cancelled.", exception.Message);
    }

    [Fact]
    public void CancelFromRequestedTransitionsToCancelledAndRaisesEvent()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();

        promotion.Cancel(" requester-user ");

        Assert.Equal(PromotionStatus.Cancelled, promotion.Status);
        var cancelledEvent = Assert.IsType<PromotionCancelledDomainEvent>(Assert.Single(promotion.DomainEvents));
        Assert.Equal(promotion.Id, cancelledEvent.PromotionId);
        Assert.Equal("requester-user", cancelledEvent.ActingUser);
        Assert.Equal("CancelPromotion", promotion.StateHistory.Last().Command);
    }

    [Fact]
    public void CreateWithSkippedEnvironmentPathThrowsDomainRuleViolation()
    {
        var action = () => Promotion.Create(
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "production",
            actingUser: "requester-user");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Environment promotion must follow fixed order without skipping: dev -> staging -> production.", exception.Message);
    }

    [Fact]
    public void CreateWithSameSourceAndTargetThrowsDomainRuleViolation()
    {
        var action = () => Promotion.Create(
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "staging",
            targetEnvironment: "staging",
            actingUser: "requester-user");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Source and target environment must be different.", exception.Message);
    }

    [Fact]
    public void CreateWithoutActingUserThrowsDomainRuleViolation()
    {
        var action = () => Promotion.Create(
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            actingUser: "   ");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Acting user is required.", exception.Message);
    }

    [Fact]
    public void PullDomainEventsReturnsAndClearsPendingEvents()
    {
        var promotion = CreateRequestedPromotion();

        var pulled = promotion.PullDomainEvents();

        Assert.Single(pulled);
        Assert.Empty(promotion.DomainEvents);
    }

    [Fact]
    public void RehydrateWithInvalidStatusThrowsDomainRuleViolation()
    {
        var action = () => Promotion.Rehydrate(
            id: Guid.NewGuid(),
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            status: (PromotionStatus)999,
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            updatedAt: DateTimeOffset.UtcNow,
            rolledBackReason: null,
            completedAt: null,
            workItems: [],
            stateHistory: []);

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Invalid promotion status persisted in storage.", exception.Message);
    }

    [Fact]
    public void RehydrateWithValidStateDoesNotRaiseDomainEvents()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        var promotion = Promotion.Rehydrate(
            id: Guid.NewGuid(),
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            status: PromotionStatus.InProgress,
            createdAt: createdAt,
            updatedAt: updatedAt,
            rolledBackReason: null,
            completedAt: null,
            workItems: [new WorkItemReference("WI-123", "Fix checkout timeout")],
            stateHistory:
            [
                new PromotionStateHistoryEntry(null, PromotionStatus.Requested, "RequestPromotion", createdAt),
                new PromotionStateHistoryEntry(PromotionStatus.Requested, PromotionStatus.Approved, "ApprovePromotion", createdAt.AddMinutes(1)),
                new PromotionStateHistoryEntry(PromotionStatus.Approved, PromotionStatus.InProgress, "StartDeployment", updatedAt)
            ]);

        Assert.Equal(PromotionStatus.InProgress, promotion.Status);
        Assert.Equal(createdAt, promotion.CreatedAt);
        Assert.Equal(updatedAt, promotion.UpdatedAt);
        Assert.Empty(promotion.DomainEvents);
    }

    private static Promotion CreateRequestedPromotion()
    {
        return Promotion.Create(
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            actingUser: "requester-user");
    }
}
