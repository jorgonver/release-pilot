namespace ReleasePilot.Api.Infrastructure.Outbox;

public sealed class OutboxPublisherOptions
{
    public const string SectionName = "OutboxPublisher";

    public int PollIntervalSeconds { get; set; } = 3;

    public int BatchSize { get; set; } = 50;

    public int RetryDelaySeconds { get; set; } = 10;

    public int MaxRetryCount { get; set; } = 10;
}
