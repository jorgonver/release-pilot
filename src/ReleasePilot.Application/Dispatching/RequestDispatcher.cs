using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Application.Dispatching;

public sealed class RequestDispatcher : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandTransactionExecutor _transactionExecutor;

    public RequestDispatcher(IServiceProvider serviceProvider, ICommandTransactionExecutor transactionExecutor)
    {
        _serviceProvider = serviceProvider;
        _transactionExecutor = transactionExecutor;
    }

    public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        return _transactionExecutor.ExecuteAsync(async ct =>
        {
            var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
            return await handler.HandleAsync(command, ct);
        }, cancellationToken);
    }

    public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return handler.HandleAsync(query, cancellationToken);
    }
}
