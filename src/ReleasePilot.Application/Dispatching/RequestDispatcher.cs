using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Application.Dispatching;

public sealed class RequestDispatcher : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public RequestDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return handler.HandleAsync(command, cancellationToken);
    }

    public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return handler.HandleAsync(query, cancellationToken);
    }
}
