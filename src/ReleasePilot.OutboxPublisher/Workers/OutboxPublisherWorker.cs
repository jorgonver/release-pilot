using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ReleasePilot.Api.Application.Promotions.Events;
using ReleasePilot.Api.Infrastructure.Messaging;
using ReleasePilot.Api.Infrastructure.Outbox;

namespace ReleasePilot.OutboxPublisher.Workers;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly OutboxPublisherOptions _outboxOptions;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IOutboxRepository outboxRepository,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<OutboxPublisherOptions> outboxOptions,
        ILogger<OutboxPublisherWorker> logger)
    {
        _outboxRepository = outboxRepository;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _outboxOptions = outboxOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected outbox publisher loop failure.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_outboxOptions.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await _outboxRepository.GetPendingBatchAsync(
            _outboxOptions.BatchSize,
            _outboxOptions.MaxRetryCount,
            cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqOptions.HostName,
            Port = _rabbitMqOptions.Port,
            UserName = _rabbitMqOptions.UserName,
            Password = _rabbitMqOptions.Password,
            VirtualHost = _rabbitMqOptions.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            _rabbitMqOptions.PromotionExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        foreach (var outboxMessage in pending)
        {
            try
            {
                var message = JsonSerializer.Deserialize<PromotionEventMessage>(outboxMessage.PayloadJson)
                    ?? throw new InvalidOperationException($"Outbox payload could not be deserialized for message '{outboxMessage.Id}'.");

                var body = Encoding.UTF8.GetBytes(outboxMessage.PayloadJson);
                var routingKey = $"{_rabbitMqOptions.PromotionRoutingKeyPrefix}.{message.EventType}";
                var properties = new BasicProperties
                {
                    Persistent = true,
                    MessageId = message.EventId.ToString(),
                    Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds())
                };

                await channel.BasicPublishAsync(
                    exchange: _rabbitMqOptions.PromotionExchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken);

                await _outboxRepository.MarkProcessedAsync(outboxMessage.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed publishing outbox message '{OutboxMessageId}'.", outboxMessage.Id);
                await _outboxRepository.MarkFailedAsync(outboxMessage.Id, ex.Message, _outboxOptions.RetryDelaySeconds, cancellationToken);
            }
        }
    }
}
