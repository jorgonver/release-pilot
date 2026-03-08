using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReleasePilot.Api.Application.Promotions.Events;

namespace ReleasePilot.AuditWorker;

public sealed class RabbitMqPromotionEventConsumer : IPromotionEventConsumer
{
    private readonly ILogger<RabbitMqPromotionEventConsumer> _logger;
    private readonly AuditWorkerOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPromotionEventConsumer(IOptions<AuditWorkerOptions> options, ILogger<RabbitMqPromotionEventConsumer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(
        Func<PromotionEventMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        _connection = await CreateConnectionAsync(cancellationToken);
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
                await onMessage(message, cancellationToken);
                await AckAsync(_channel, eventArgs.DeliveryTag, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing promotion event message.");
                await NackAsync(_channel, eventArgs.DeliveryTag, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _options.RabbitMq.AuditQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Connected to RabbitMQ and consuming queue '{QueueName}'.", _options.RabbitMq.AuditQueueName);
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

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _options.RabbitMq.HostName,
            Port = _options.RabbitMq.Port,
            UserName = _options.RabbitMq.UserName,
            Password = _options.RabbitMq.Password,
            VirtualHost = _options.RabbitMq.VirtualHost
        };
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var factory = CreateConnectionFactory();
        return await factory.CreateConnectionAsync(cancellationToken);
    }

    private async Task EnsureTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            _options.RabbitMq.PromotionExchange,
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

        return message;
    }

    private static ValueTask AckAsync(IChannel channel, ulong deliveryTag, CancellationToken cancellationToken)
    {
        return channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken);
    }

    private static ValueTask NackAsync(IChannel channel, ulong deliveryTag, CancellationToken cancellationToken)
    {
        return channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
    }
}
