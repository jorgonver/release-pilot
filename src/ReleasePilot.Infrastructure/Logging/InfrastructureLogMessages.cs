namespace ReleasePilot.Api.Infrastructure.Logging;

internal static partial class InfrastructureLogMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "NoOp deployment invoked for promotion {PromotionId} ({ApplicationName} {Version}: {Source}->{Target})")]
    public static partial void NoOpDeploymentInvoked(
        ILogger logger,
        Guid promotionId,
        string applicationName,
        string version,
        string source,
        string target);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "PromotionRequested: {PromotionId} {ApplicationName} {Version} ({Source}->{Target})")]
    public static partial void PromotionRequested(
        ILogger logger,
        Guid promotionId,
        string applicationName,
        string version,
        string source,
        string target);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "PromotionApproved: {PromotionId}")]
    public static partial void PromotionApproved(ILogger logger, Guid promotionId);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "DeploymentStarted: {PromotionId}")]
    public static partial void DeploymentStarted(ILogger logger, Guid promotionId);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "PromotionCompleted: {PromotionId}")]
    public static partial void PromotionCompleted(ILogger logger, Guid promotionId);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "PromotionRolledBack: {PromotionId}, Reason: {Reason}")]
    public static partial void PromotionRolledBack(ILogger logger, Guid promotionId, string reason);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information, Message = "PromotionCancelled: {PromotionId}")]
    public static partial void PromotionCancelled(ILogger logger, Guid promotionId);

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Notification stub: Promotion {PromotionId} reached terminal state {TerminalState}. Reason: {Reason}")]
    public static partial void NotificationStub(
        ILogger logger,
        Guid promotionId,
        string terminalState,
        string reason);
}