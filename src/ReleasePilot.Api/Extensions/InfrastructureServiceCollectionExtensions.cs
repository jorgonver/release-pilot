using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions.Events;
using ReleasePilot.Api.Domain.Promotions.Events;
using ReleasePilot.Api.Infrastructure.Messaging;
using ReleasePilot.Api.Infrastructure.Outbox;
using ReleasePilot.Api.Infrastructure.Persistence;
using ReleasePilot.Api.Infrastructure.Ports;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace ReleasePilot.Api.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var externalPortsSettings = configuration.GetSection(ExternalPortsOptions.SectionName).Get<ExternalPortsOptions>() ?? new ExternalPortsOptions();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services
            .AddOptions<PromotionRepositoryOptions>()
            .Bind(configuration.GetSection(PromotionRepositoryOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                $"{PromotionRepositoryOptions.SectionName}:ConnectionString must be configured.")
            .ValidateOnStart();

        services
            .AddOptions<ExternalPortsOptions>()
            .Bind(configuration.GetSection(ExternalPortsOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient<HttpDeploymentPort>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ExternalPortsOptions>>().Value;
            ConfigureHttpClient(client, options.Deployment);
        }).AddStandardResilienceHandler(options =>
        {
            ConfigureResilienceOptions(
                options,
                externalPortsSettings.Deployment.Resilience);
        });

        services.AddHttpClient<HttpIssueTrackerPort>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ExternalPortsOptions>>().Value;
            ConfigureHttpClient(client, options.IssueTracker);
        }).AddStandardResilienceHandler(options =>
        {
            ConfigureResilienceOptions(
                options,
                externalPortsSettings.IssueTracker.Resilience);
        });

        services.AddHttpClient<HttpNotificationPort>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ExternalPortsOptions>>().Value;
            ConfigureHttpClient(client, options.Notification);
        }).AddStandardResilienceHandler(options =>
        {
            ConfigureResilienceOptions(
                options,
                externalPortsSettings.Notification.Resilience);
        });

        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<ICommandTransactionExecutor, CommandTransactionExecutor>();
        services.AddSingleton<IOutboxRepository, OutboxRepository>();

        services.AddSingleton<NoOpDeploymentPort>();
        services.AddSingleton<InMemoryIssueTrackerPort>();
        services.AddSingleton<InMemoryNotificationPort>();

        services.AddSingleton<IDeploymentPort>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ExternalPortsOptions>>().Value;
            return options.UseHttpMode
                ? sp.GetRequiredService<HttpDeploymentPort>()
                : sp.GetRequiredService<NoOpDeploymentPort>();
        });

        services.AddSingleton<IIssueTrackerPort>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ExternalPortsOptions>>().Value;
            return options.UseHttpMode
                ? sp.GetRequiredService<HttpIssueTrackerPort>()
                : sp.GetRequiredService<InMemoryIssueTrackerPort>();
        });

        services.AddSingleton<INotificationPort>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ExternalPortsOptions>>().Value;
            return options.UseHttpMode
                ? sp.GetRequiredService<HttpNotificationPort>()
                : sp.GetRequiredService<InMemoryNotificationPort>();
        });

        services.AddScoped<PromotionLifecycleLoggingEventHandler>();
        services.AddScoped<PromotionOutboxEventHandler>();
        services.AddScoped<PromotionTerminalStateNotificationHandler>();

        // Register domain event handlers for each promotion lifecycle event:
        
        // Promotion requested domain event
        services.AddScoped<IDomainEventHandler<PromotionRequestedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionRequestedDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());
        
        // Promotion approved domain event
        services.AddScoped<IDomainEventHandler<PromotionApprovedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionApprovedDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        // Deployment started domain event
        services.AddScoped<IDomainEventHandler<DeploymentStartedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<DeploymentStartedDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        // Promotion completed domain event
        services.AddScoped<IDomainEventHandler<PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        // Promotion rolled back domain event
        services.AddScoped<IDomainEventHandler<PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        // Promotion cancelled domain event
        services.AddScoped<IDomainEventHandler<PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
        services.AddScoped<IDomainEventHandler<PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<PromotionOutboxEventHandler>());

        // Promotion terminal state domain events
        services.AddScoped<IDomainEventHandler<PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());
        services.AddScoped<IDomainEventHandler<PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());
        services.AddScoped<IDomainEventHandler<PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());

        return services;
    }

    private static void ConfigureHttpClient(HttpClient client, ExternalServiceOptions options)
    {
        if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            client.BaseAddress = baseUri;
        }

        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
    }

    private static void ConfigureResilienceOptions(
        HttpStandardResilienceOptions resilienceOptions,
        ExternalServiceResilienceOptions configured)
    {
        resilienceOptions.Retry.MaxRetryAttempts = Math.Max(0, configured.RetryMaxAttempts);
        resilienceOptions.CircuitBreaker.MinimumThroughput = Math.Max(2, configured.CircuitBreakerMinimumThroughput);
        resilienceOptions.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(1, configured.CircuitBreakerSamplingSeconds));
        resilienceOptions.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(Math.Max(1, configured.CircuitBreakerBreakSeconds));
        resilienceOptions.CircuitBreaker.FailureRatio = Math.Clamp(configured.CircuitBreakerFailureRatio, 0.01, 1.0);
        resilienceOptions.AttemptTimeout.Timeout = TimeSpan.FromSeconds(Math.Max(1, configured.AttemptTimeoutSeconds));
        resilienceOptions.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(Math.Max(1, configured.TotalTimeoutSeconds));
    }
}
