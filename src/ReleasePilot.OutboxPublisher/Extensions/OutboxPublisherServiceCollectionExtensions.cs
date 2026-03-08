using ReleasePilot.Api.Infrastructure.Messaging;
using ReleasePilot.Api.Infrastructure.Outbox;
using ReleasePilot.Api.Infrastructure.Persistence;
using ReleasePilot.OutboxPublisher.Workers;

namespace ReleasePilot.OutboxPublisher.Extensions;

public static class OutboxPublisherServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxPublisherLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<OutboxPublisherOptions>(configuration.GetSection(OutboxPublisherOptions.SectionName));

        services
            .AddOptions<PromotionRepositoryOptions>()
            .Bind(configuration.GetSection(PromotionRepositoryOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                $"{PromotionRepositoryOptions.SectionName}:ConnectionString must be configured.")
            .ValidateOnStart();

        services.AddSingleton<IOutboxRepository, OutboxRepository>();
        services.AddHostedService<OutboxPublisherWorker>();

        return services;
    }
}
