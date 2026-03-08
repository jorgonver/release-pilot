using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions.Events;
using ReleasePilot.Api.Domain.Promotions.Events;
using ReleasePilot.Api.Infrastructure.Messaging;
using ReleasePilot.Api.Infrastructure.Outbox;
using ReleasePilot.Api.Infrastructure.Persistence;
using ReleasePilot.Api.Infrastructure.Ports;

namespace ReleasePilot.Api.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services
            .AddOptions<PromotionRepositoryOptions>()
            .Bind(configuration.GetSection(PromotionRepositoryOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                $"{PromotionRepositoryOptions.SectionName}:ConnectionString must be configured.")
            .ValidateOnStart();

        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<ICommandTransactionExecutor, CommandTransactionExecutor>();
        services.AddSingleton<IOutboxRepository, OutboxRepository>();
        services.AddSingleton<IDeploymentPort, NoOpDeploymentPort>();
        services.AddSingleton<IIssueTrackerPort, InMemoryIssueTrackerPort>();
        services.AddSingleton<INotificationPort, InMemoryNotificationPort>();

        services.AddScoped<PromotionLifecycleLoggingEventHandler>();
        services.AddScoped<PromotionOutboxEventHandler>();
        services.AddScoped<PromotionTerminalStateNotificationHandler>();

        services.AddScoped<IDomainEventHandler<PromotionRequestedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionRequestedDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        services.AddScoped<IDomainEventHandler<PromotionApprovedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionApprovedDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        services.AddScoped<IDomainEventHandler<DeploymentStartedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<DeploymentStartedDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        services.AddScoped<IDomainEventHandler<PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        services.AddScoped<IDomainEventHandler<PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        services.AddScoped<IDomainEventHandler<PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        services.AddScoped<IDomainEventHandler<PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());
        services.AddScoped<IDomainEventHandler<PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());
        services.AddScoped<IDomainEventHandler<PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());

        return services;
    }
}
