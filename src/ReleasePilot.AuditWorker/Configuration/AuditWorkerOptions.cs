namespace ReleasePilot.AuditWorker;

public sealed class AuditWorkerOptions
{
    public const string SectionName = "AuditWorker";

    public RabbitMqSettings RabbitMq { get; set; } = new();

    public PostgresSettings Postgres { get; set; } = new();
}

public sealed class PostgresSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public string PromotionExchange { get; set; } = "releasepilot.promotions";

    public string AuditQueueName { get; set; } = "releasepilot.audit";

    public string AuditBindingKey { get; set; } = "promotion.#";

    public int MaxProcessingRetries { get; set; } = 3;

    public string RetryExchange { get; set; } = "releasepilot.promotions.retry";

    public string RetryQueueName { get; set; } = "releasepilot.audit.retry";

    public string RetryRoutingKey { get; set; } = "promotion.audit.retry";

    public int RetryDelaySeconds { get; set; } = 10;

    public string DeadLetterExchange { get; set; } = "releasepilot.promotions.dlx";

    public string DeadLetterQueueName { get; set; } = "releasepilot.audit.dlq";

    public string DeadLetterRoutingKey { get; set; } = "promotion.audit.dead";
}
