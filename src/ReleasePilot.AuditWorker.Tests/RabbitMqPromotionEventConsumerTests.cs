using System.Text;
using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReleasePilot.Api.Application.Promotions.Events;

namespace ReleasePilot.AuditWorker.Tests;

public sealed class RabbitMqPromotionEventConsumerTests
{
    [Fact]
    public async Task StartAsyncDeclaresRetryTopology()
    {
        var context = CreateContext();
        var sut = context.CreateSut();

        await sut.StartAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        var retryExchangeDeclareCalls = context.Channel
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IChannel.ExchangeDeclareAsync))
            .Where(call => Equals(call.GetArguments()[0], context.Options.RabbitMq.RetryExchange))
            .ToList();

        Assert.Single(retryExchangeDeclareCalls);

        var retryQueueDeclareCall = context.Channel
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IChannel.QueueDeclareAsync))
            .FirstOrDefault(call => Equals(call.GetArguments()[0], context.Options.RabbitMq.RetryQueueName));

        Assert.NotNull(retryQueueDeclareCall);

        var args = retryQueueDeclareCall.GetArguments()[4] as IDictionary<string, object?>;
        Assert.NotNull(args);
        Assert.True(args.ContainsKey("x-message-ttl"));
        Assert.Equal(
            context.Options.RabbitMq.RetryDelaySeconds * 1000,
            Convert.ToInt32(args["x-message-ttl"], CultureInfo.InvariantCulture));
        Assert.Equal(context.Options.RabbitMq.PromotionExchange, args["x-dead-letter-exchange"]);
        Assert.Equal(context.Options.RabbitMq.AuditBindingKey, args["x-dead-letter-routing-key"]);
    }

    [Fact]
    public async Task FailedMessageUnderRetryLimitIsPublishedToRetryExchange()
    {
        var context = CreateContext();
        var sut = context.CreateSut();

        await sut.StartAsync((_, _) => throw new InvalidOperationException("boom"), CancellationToken.None);

        var eventArgs = CreateEventArgs(retryCount: 0);
        await context.Consumer!.HandleBasicDeliverAsync(
            "ctag",
            12,
            false,
            context.Options.RabbitMq.PromotionExchange,
            context.Options.RabbitMq.AuditBindingKey,
            eventArgs.BasicProperties!,
            eventArgs.Body,
            CancellationToken.None);

        AssertPublished(context.Channel, context.Options.RabbitMq.RetryExchange, context.Options.RabbitMq.RetryRoutingKey, 1);
        AssertPublished(context.Channel, context.Options.RabbitMq.DeadLetterExchange, context.Options.RabbitMq.DeadLetterRoutingKey, 0);
    }

    [Fact]
    public async Task FailedMessageAtRetryLimitIsPublishedToDeadLetterExchange()
    {
        var context = CreateContext();
        context.Options.RabbitMq.MaxProcessingRetries = 2;

        var sut = context.CreateSut();
        await sut.StartAsync((_, _) => throw new InvalidOperationException("boom"), CancellationToken.None);

        var eventArgs = CreateEventArgs(retryCount: 2);
        await context.Consumer!.HandleBasicDeliverAsync(
            "ctag",
            14,
            false,
            context.Options.RabbitMq.PromotionExchange,
            context.Options.RabbitMq.AuditBindingKey,
            eventArgs.BasicProperties!,
            eventArgs.Body,
            CancellationToken.None);

        AssertPublished(context.Channel, context.Options.RabbitMq.DeadLetterExchange, context.Options.RabbitMq.DeadLetterRoutingKey, 1);
    }

    private static void AssertPublished(IChannel channel, string exchange, string routingKey, int expectedCount)
    {
        var publishCalls = channel
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IChannel.BasicPublishAsync))
            .Where(call => Equals(call.GetArguments()[0], exchange))
            .Where(call => Equals(call.GetArguments()[1], routingKey))
            .ToList();

        Assert.Equal(expectedCount, publishCalls.Count);
    }

    private static ConsumerTestContext CreateContext()
    {
        var options = new AuditWorkerOptions
        {
            RabbitMq = new RabbitMqSettings
            {
                PromotionExchange = "releasepilot.promotions",
                AuditQueueName = "releasepilot.audit",
                AuditBindingKey = "promotion.#",
                RetryExchange = "releasepilot.promotions.retry",
                RetryQueueName = "releasepilot.audit.retry",
                RetryRoutingKey = "promotion.audit.retry",
                RetryDelaySeconds = 10,
                MaxProcessingRetries = 3,
                DeadLetterExchange = "releasepilot.promotions.dlx",
                DeadLetterQueueName = "releasepilot.audit.dlq",
                DeadLetterRoutingKey = "promotion.audit.dead"
            }
        };

        var channel = Substitute.For<IChannel>();
        channel.BasicConsumeAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<IAsyncBasicConsumer>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("ctag"));

        IAsyncBasicConsumer? capturedConsumer = null;
        channel.When(call => call.BasicConsumeAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<IAsyncBasicConsumer>(),
            Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                capturedConsumer = callInfo.ArgAt<IAsyncBasicConsumer>(6);
            });

        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(default).ReturnsForAnyArgs(channel);

        var connectionFactory = Substitute.For<IRabbitMqConnectionFactory>();
        connectionFactory.CreateConnectionAsync(Arg.Any<CancellationToken>()).Returns(connection);

        return new ConsumerTestContext(options, channel, connectionFactory, () => capturedConsumer);
    }

    private static BasicDeliverEventArgs CreateEventArgs(int retryCount)
    {
        var message = new PromotionEventMessage(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            "PromotionRequested",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "system",
            "{}");

        var payload = JsonSerializer.Serialize(message);
        var properties = new BasicProperties
        {
            CorrelationId = message.CorrelationId,
            ContentType = "application/json",
            Headers = new Dictionary<string, object?>
            {
                ["x-releasepilot-retry-count"] = retryCount
            }
        };

        return new BasicDeliverEventArgs(
            "ctag",
            1,
            false,
            "releasepilot.promotions",
            "promotion.created",
            properties,
            Encoding.UTF8.GetBytes(payload),
            CancellationToken.None);
    }

    private sealed class ConsumerTestContext
    {
        private readonly Func<IAsyncBasicConsumer?> _consumerAccessor;

        public ConsumerTestContext(
            AuditWorkerOptions options,
            IChannel channel,
            IRabbitMqConnectionFactory connectionFactory,
            Func<IAsyncBasicConsumer?> consumerAccessor)
        {
            Options = options;
            Channel = channel;
            ConnectionFactory = connectionFactory;
            _consumerAccessor = consumerAccessor;
        }

        public AuditWorkerOptions Options { get; }

        public IChannel Channel { get; }

        public IRabbitMqConnectionFactory ConnectionFactory { get; }

        public AsyncEventingBasicConsumer? Consumer => _consumerAccessor() as AsyncEventingBasicConsumer;

        public RabbitMqPromotionEventConsumer CreateSut()
        {
            return new RabbitMqPromotionEventConsumer(
                Microsoft.Extensions.Options.Options.Create(Options),
                ConnectionFactory,
                NullLogger<RabbitMqPromotionEventConsumer>.Instance);
        }
    }
}
