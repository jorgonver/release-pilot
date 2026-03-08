namespace ReleasePilot.AuditWorker.Extensions;

public static class AuditWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddAuditWorkerLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AuditWorkerOptions>()
            .Bind(configuration.GetSection(AuditWorkerOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Postgres.ConnectionString),
                $"{AuditWorkerOptions.SectionName}:Postgres:ConnectionString must be configured.")
            .ValidateOnStart();

        services.AddSingleton<IAuditLogRepository, AuditLogRepository>();
        services.AddSingleton<IPromotionEventConsumer, RabbitMqPromotionEventConsumer>();
        services.AddHostedService<AuditLogConsumerWorker>();

        return services;
    }
}
