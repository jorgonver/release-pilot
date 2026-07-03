using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReleasePilot.Api.Application.Promotions.Events;
using ReleasePilot.AuditWorker.Logging;

namespace ReleasePilot.AuditWorker;

public sealed class RabbitMqPromotionEventConsumer : IPromotionEventConsumer
{
    private const string RetryHeader = "x-releasepilot-retry-count";

    private readonly ILogger<RabbitMqPromotionEventConsumer> _logger;
    private readonly AuditWorkerOptions _options;
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPromotionEventConsumer(
        IOptions<AuditWorkerOptions> options,
        IRabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqPromotionEventConsumer> logger)
    {
        _options = options.Value;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task StartAsync(
        Func<PromotionEventMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await EnsureTopologyAsync(_channel, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            if (_channel is null)
            {
                return;
            }

            try
            {
                var message = DeserializeMessage(eventArgs.Body.ToArray());
                var correlationId = eventArgs.BasicProperties?.CorrelationId;
                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    message = message with { CorrelationId = correlationId };
                }

                await onMessage(message, cancellationToken);
                await AckAsync(_channel, eventArgs.DeliveryTag, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing promotion event message.");

                var retryCount = GetRetryCount(eventArgs.BasicProperties);
                var maxRetries = Math.Max(0, _options.RabbitMq.MaxProcessingRetries);

                if (retryCount < maxRetries)
                {
                    var nextAttempt = retryCount + 1;
                    AuditWorkerLogMessages.RetryingMessage(_logger, nextAttempt, maxRetries);

                    await PublishRetryAsync(_channel, eventArgs, nextAttempt, cancellationToken);
                    await AckAsync(_channel, eventArgs.DeliveryTag, cancellationToken);
                    return;
                }

                AuditWorkerLogMessages.MessageMovedToDeadLetter(_logger, maxRetries);
                await PublishToDeadLetterAsync(_channel, eventArgs, cancellationToken);
                await AckAsync(_channel, eventArgs.DeliveryTag, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _options.RabbitMq.AuditQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        AuditWorkerLogMessages.ConnectedToRabbitMq(_logger, _options.RabbitMq.AuditQueueName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private async Task EnsureTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            _options.RabbitMq.PromotionExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            _options.RabbitMq.DeadLetterExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            _options.RabbitMq.RetryExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            _options.RabbitMq.AuditQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            _options.RabbitMq.AuditQueueName,
            _options.RabbitMq.PromotionExchange,
            _options.RabbitMq.AuditBindingKey,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            _options.RabbitMq.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            _options.RabbitMq.DeadLetterQueueName,
            _options.RabbitMq.DeadLetterExchange,
            _options.RabbitMq.DeadLetterRoutingKey,
            cancellationToken: cancellationToken);

        var retryQueueArguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = Math.Max(1000, _options.RabbitMq.RetryDelaySeconds * 1000),
            ["x-dead-letter-exchange"] = _options.RabbitMq.PromotionExchange,
            ["x-dead-letter-routing-key"] = _options.RabbitMq.AuditBindingKey
        };

        await channel.QueueDeclareAsync(
            _options.RabbitMq.RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryQueueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            _options.RabbitMq.RetryQueueName,
            _options.RabbitMq.RetryExchange,
            _options.RabbitMq.RetryRoutingKey,
            cancellationToken: cancellationToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: cancellationToken);
    }

    private static PromotionEventMessage DeserializeMessage(byte[] body)
    {
        var json = Encoding.UTF8.GetString(body);
        var message = JsonSerializer.Deserialize<PromotionEventMessage>(json);

        if (message is null)
        {
            throw new InvalidOperationException("Received empty promotion event message.");
        }

        if (string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            return message with { CorrelationId = Guid.NewGuid().ToString("N") };
        }

        return message;
    }

    private static ValueTask AckAsync(IChannel channel, ulong deliveryTag, CancellationToken cancellationToken)
    {
        return channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken);
    }

    private static int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers is null)
        {
            return 0;
        }

        if (!properties.Headers.TryGetValue(RetryHeader, out var rawValue) || rawValue is null)
        {
            return 0;
        }

        return rawValue switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            _ => 0
        };
    }

    private async Task PublishRetryAsync(IChannel channel, BasicDeliverEventArgs eventArgs, int nextAttempt, CancellationToken cancellationToken)
    {
        var properties = CreateForwardProperties(eventArgs.BasicProperties, nextAttempt);

        await channel.BasicPublishAsync(
            exchange: _options.RabbitMq.RetryExchange,
            routingKey: _options.RabbitMq.RetryRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: eventArgs.Body,
            cancellationToken: cancellationToken);
    }

    private async Task PublishToDeadLetterAsync(IChannel channel, BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var retryCount = GetRetryCount(eventArgs.BasicProperties);
        var properties = CreateForwardProperties(eventArgs.BasicProperties, retryCount);

        await channel.BasicPublishAsync(
            exchange: _options.RabbitMq.DeadLetterExchange,
            routingKey: _options.RabbitMq.DeadLetterRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: eventArgs.Body,
            cancellationToken: cancellationToken);
    }

    private static BasicProperties CreateForwardProperties(IReadOnlyBasicProperties? sourceProperties, int retryCount)
    {
        var properties = new BasicProperties
        {
            ContentType = sourceProperties?.ContentType,
            CorrelationId = sourceProperties?.CorrelationId,
            MessageId = sourceProperties?.MessageId,
            Type = sourceProperties?.Type,
            Persistent = true,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (sourceProperties?.Headers is not null)
        {
            foreach (var pair in sourceProperties.Headers)
            {
                headers[pair.Key] = pair.Value;
            }
        }

        headers[RetryHeader] = retryCount;
        properties.Headers = headers;

        return properties;
    }

}
