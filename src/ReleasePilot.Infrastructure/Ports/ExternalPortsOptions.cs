namespace ReleasePilot.Api.Infrastructure.Ports;

public sealed class ExternalPortsOptions
{
    public const string SectionName = "ExternalPorts";

    public string Mode { get; set; } = "Stub";

    public ExternalServiceOptions Deployment { get; set; } = new();

    public ExternalServiceOptions IssueTracker { get; set; } = new();

    public ExternalServiceOptions Notification { get; set; } = new();

    public bool UseHttpMode => string.Equals(Mode, "Http", StringComparison.OrdinalIgnoreCase);
}

public sealed class ExternalServiceOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 5;

    public ExternalServiceResilienceOptions Resilience { get; set; } = new();
}

public sealed class ExternalServiceResilienceOptions
{
    public int RetryMaxAttempts { get; set; } = 3;

    public int CircuitBreakerMinimumThroughput { get; set; } = 5;

    public int CircuitBreakerSamplingSeconds { get; set; } = 30;

    public int CircuitBreakerBreakSeconds { get; set; } = 30;

    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    public int AttemptTimeoutSeconds { get; set; } = 3;

    public int TotalTimeoutSeconds { get; set; } = 10;
}
