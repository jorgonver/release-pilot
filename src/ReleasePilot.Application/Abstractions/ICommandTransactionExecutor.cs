namespace ReleasePilot.Api.Application.Abstractions;

public interface ICommandTransactionExecutor
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
