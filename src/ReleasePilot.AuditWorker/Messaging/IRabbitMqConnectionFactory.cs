using RabbitMQ.Client;

namespace ReleasePilot.AuditWorker;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken);
}

public sealed class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly AuditWorkerOptions _options;

    public RabbitMqConnectionFactory(AuditWorkerOptions options)
    {
        _options = options;
    }

    public Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.RabbitMq.HostName,
            Port = _options.RabbitMq.Port,
            UserName = _options.RabbitMq.UserName,
            Password = _options.RabbitMq.Password,
            VirtualHost = _options.RabbitMq.VirtualHost
        };

        return factory.CreateConnectionAsync(cancellationToken);
    }
}
