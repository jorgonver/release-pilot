using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions.Events;
using ReleasePilot.Api.Domain.Promotions.Events;
using ReleasePilot.Api.Infrastructure.Messaging;

namespace ReleasePilot.Api.Infrastructure.Events;

public sealed class PromotionRabbitMqPublisherEventHandler :
    IDomainEventHandler<PromotionRequestedDomainEvent>,
    IDomainEventHandler<PromotionApprovedDomainEvent>,
    IDomainEventHandler<DeploymentStartedDomainEvent>,
    IDomainEventHandler<PromotionCompletedDomainEvent>,
    IDomainEventHandler<PromotionRolledBackDomainEvent>,
    IDomainEventHandler<PromotionCancelledDomainEvent>
{
    private readonly RabbitMqOptions _options;

    public PromotionRabbitMqPublisherEventHandler(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public Task HandleAsync(PromotionRequestedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return PublishAsync(domainEvent, "requested", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(PromotionApprovedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return PublishAsync(domainEvent, "approved", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(DeploymentStartedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return PublishAsync(domainEvent, "deployment_started", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(PromotionCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return PublishAsync(domainEvent, "completed", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(PromotionRolledBackDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return PublishAsync(domainEvent, "rolled_back", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    public Task HandleAsync(PromotionCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return PublishAsync(domainEvent, "cancelled", domainEvent.PromotionId, domainEvent.ActingUser, domainEvent.OccurredAt, cancellationToken);
    }

    private async Task PublishAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        string eventType,
        Guid promotionId,
        string actingUser,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = new PromotionEventMessage(
            Guid.NewGuid(),
            eventType,
            promotionId,
            occurredAt,
            actingUser,
            JsonSerializer.Serialize(domainEvent));

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(_options.PromotionExchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var routingKey = $"{_options.PromotionRoutingKeyPrefix}.{eventType}";
        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = message.EventId.ToString(),
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            exchange: _options.PromotionExchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
