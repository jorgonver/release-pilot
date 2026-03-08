using ReleasePilot.Api.Domain.Primitives;
using ReleasePilot.Api.Domain.Promotions.Events;

namespace ReleasePilot.Api.Domain.Promotions;

public sealed class Promotion : AggregateRoot
{
    private readonly List<WorkItemReference> _workItems = new();
    private readonly List<PromotionStateHistoryEntry> _stateHistory = new();

    private Promotion(
        Guid id,
        string applicationName,
        string version,
        string sourceEnvironment,
        string targetEnvironment,
        string actingUser,
        IReadOnlyCollection<WorkItemReference> workItems)
    {
        Id = id;
        ApplicationName = applicationName;
        Version = version;
        SourceEnvironment = sourceEnvironment;
        TargetEnvironment = targetEnvironment;
        _workItems.AddRange(workItems);
        Status = PromotionStatus.Requested;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        RolledBackReason = null;
        CompletedAt = null;

        _stateHistory.Add(new PromotionStateHistoryEntry(null, PromotionStatus.Requested, "RequestPromotion", CreatedAt));

        AddDomainEvent(new PromotionRequestedDomainEvent(Id, ApplicationName, Version, SourceEnvironment, TargetEnvironment, actingUser));
    }

    private Promotion(
        Guid id,
        string applicationName,
        string version,
        string sourceEnvironment,
        string targetEnvironment,
        PromotionStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? rolledBackReason,
        DateTimeOffset? completedAt,
        IReadOnlyCollection<WorkItemReference> workItems,
        IReadOnlyCollection<PromotionStateHistoryEntry> stateHistory)
    {
        Id = id;
        ApplicationName = applicationName;
        Version = version;
        SourceEnvironment = sourceEnvironment;
        TargetEnvironment = targetEnvironment;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        RolledBackReason = rolledBackReason;
        CompletedAt = completedAt;
        _workItems.AddRange(workItems);
        _stateHistory.AddRange(stateHistory);
    }

    public Guid Id { get; }

    public string ApplicationName { get; }

    public string Version { get; }

    public string SourceEnvironment { get; }

    public string TargetEnvironment { get; }

    public PromotionStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? RolledBackReason { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<WorkItemReference> WorkItems => _workItems.AsReadOnly();

    public IReadOnlyCollection<PromotionStateHistoryEntry> StateHistory => _stateHistory.AsReadOnly();

    public static Promotion Create(
        string applicationName,
        string version,
        string sourceEnvironment,
        string targetEnvironment,
        string actingUser,
        IReadOnlyCollection<WorkItemReference>? workItems = null)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new DomainRuleViolationException("Application name is required.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DomainRuleViolationException("Version is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceEnvironment))
        {
            throw new DomainRuleViolationException("Source environment is required.");
        }

        if (string.IsNullOrWhiteSpace(targetEnvironment))
        {
            throw new DomainRuleViolationException("Target environment is required.");
        }

        if (sourceEnvironment.Trim().Equals(targetEnvironment.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleViolationException("Source and target environment must be different.");
        }

        EnvironmentPromotionPolicy.EnsureKnown(sourceEnvironment, nameof(sourceEnvironment));
        EnvironmentPromotionPolicy.EnsureKnown(targetEnvironment, nameof(targetEnvironment));
        EnvironmentPromotionPolicy.EnsureAdjacentPromotionPath(sourceEnvironment, targetEnvironment);

        var normalizedActingUser = NormalizeActingUser(actingUser);
        var normalizedWorkItems = workItems ?? Array.Empty<WorkItemReference>();

        return new Promotion(
            Guid.NewGuid(),
            applicationName.Trim(),
            version.Trim(),
            EnvironmentPromotionPolicy.Normalize(sourceEnvironment),
            EnvironmentPromotionPolicy.Normalize(targetEnvironment),
            normalizedActingUser,
            normalizedWorkItems);
    }

    public static Promotion Rehydrate(
        Guid id,
        string applicationName,
        string version,
        string sourceEnvironment,
        string targetEnvironment,
        PromotionStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? rolledBackReason,
        DateTimeOffset? completedAt,
        IReadOnlyCollection<WorkItemReference> workItems,
        IReadOnlyCollection<PromotionStateHistoryEntry> stateHistory)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainRuleViolationException("Invalid promotion status persisted in storage.");
        }

        return new Promotion(
            id,
            applicationName,
            version,
            sourceEnvironment,
            targetEnvironment,
            status,
            createdAt,
            updatedAt,
            rolledBackReason,
            completedAt,
            workItems,
            stateHistory);
    }

    public void Approve(string approverRole, string actingUser)
    {
        EnsureState(PromotionStatus.Requested, "Only requested promotions can be approved.");
        if (!string.Equals(approverRole?.Trim(), "Approver", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleViolationException("Only users with Approver role may approve a promotion.");
        }

        var normalizedActingUser = NormalizeActingUser(actingUser);
        var previousState = Status;
        Status = PromotionStatus.Approved;
        UpdatedAt = DateTimeOffset.UtcNow;
        _stateHistory.Add(new PromotionStateHistoryEntry(previousState, Status, "ApprovePromotion", UpdatedAt));
        AddDomainEvent(new PromotionApprovedDomainEvent(Id, normalizedActingUser));
    }

    public void Start(string actingUser)
    {
        EnsureState(PromotionStatus.Approved, "Only approved promotions can be started.");
        var normalizedActingUser = NormalizeActingUser(actingUser);
        var previousState = Status;
        Status = PromotionStatus.InProgress;
        UpdatedAt = DateTimeOffset.UtcNow;
        _stateHistory.Add(new PromotionStateHistoryEntry(previousState, Status, "StartDeployment", UpdatedAt));
        AddDomainEvent(new DeploymentStartedDomainEvent(Id, normalizedActingUser));
    }

    public void Complete(string actingUser)
    {
        EnsureState(PromotionStatus.InProgress, "Only in-progress promotions can be completed.");
        var normalizedActingUser = NormalizeActingUser(actingUser);
        var previousState = Status;
        Status = PromotionStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
        CompletedAt = UpdatedAt;
        _stateHistory.Add(new PromotionStateHistoryEntry(previousState, Status, "CompletePromotion", UpdatedAt));
        AddDomainEvent(new PromotionCompletedDomainEvent(Id, normalizedActingUser));
    }

    public void Rollback(string reason, string actingUser)
    {
        EnsureState(PromotionStatus.InProgress, "Only in-progress promotions can be rolled back.");
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("Rollback reason is required.");
        }

        var normalizedActingUser = NormalizeActingUser(actingUser);
        var previousState = Status;
        RolledBackReason = reason.Trim();
        Status = PromotionStatus.RolledBack;
        UpdatedAt = DateTimeOffset.UtcNow;
        _stateHistory.Add(new PromotionStateHistoryEntry(previousState, Status, "RollbackPromotion", UpdatedAt, RolledBackReason));
        AddDomainEvent(new PromotionRolledBackDomainEvent(Id, RolledBackReason, normalizedActingUser));
    }

    public void Cancel(string actingUser)
    {
        EnsureState(PromotionStatus.Requested, "Only requested promotions can be cancelled.");
        var normalizedActingUser = NormalizeActingUser(actingUser);
        var previousState = Status;
        Status = PromotionStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
        _stateHistory.Add(new PromotionStateHistoryEntry(previousState, Status, "CancelPromotion", UpdatedAt));
        AddDomainEvent(new PromotionCancelledDomainEvent(Id, normalizedActingUser));
    }

    private void EnsureState(PromotionStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainRuleViolationException(message);
        }
    }

    private static string NormalizeActingUser(string actingUser)
    {
        if (string.IsNullOrWhiteSpace(actingUser))
        {
            throw new DomainRuleViolationException("Acting user is required.");
        }

        return actingUser.Trim();
    }
}
