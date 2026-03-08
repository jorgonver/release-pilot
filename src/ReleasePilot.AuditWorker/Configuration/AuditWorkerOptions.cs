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
}
