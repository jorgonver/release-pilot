using System.Transactions;
using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Infrastructure.Persistence;

public sealed class CommandTransactionExecutor : ICommandTransactionExecutor
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted
        };

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            transactionOptions,
            TransactionScopeAsyncFlowOption.Enabled);

        var result = await operation(cancellationToken);
        scope.Complete();
        return result;
    }
}
