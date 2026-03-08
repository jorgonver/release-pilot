using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Dispatching;

namespace ReleasePilot.Application.Tests;

public class RequestDispatcherTests
{
    [Fact]
    public async Task SendCommandAsync_ExecutesInsideTransactionAndCallsCommandHandler()
    {
        var services = new ServiceCollection();
        var commandHandler = Substitute.For<ICommandHandler<TestCommand, string>>();
        var transactionExecutor = Substitute.For<ICommandTransactionExecutor>();
        var cancellationToken = new CancellationTokenSource().Token;

        commandHandler
            .HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns("ok");

        transactionExecutor
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.ArgAt<Func<CancellationToken, Task<string>>>(0);
                var ct = callInfo.ArgAt<CancellationToken>(1);
                return operation(ct);
            });

        services.AddSingleton(commandHandler);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new RequestDispatcher(serviceProvider, transactionExecutor);

        var result = await dispatcher.SendCommandAsync<TestCommand, string>(new TestCommand("deploy"), cancellationToken);

        Assert.Equal("ok", result);
        await transactionExecutor.Received(1)
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), cancellationToken);
        await commandHandler.Received(1).HandleAsync(Arg.Is<TestCommand>(c => c.Value == "deploy"), cancellationToken);
    }

    [Fact]
    public async Task SendQueryAsync_CallsQueryHandlerWithoutTransactionExecutor()
    {
        var services = new ServiceCollection();
        var queryHandler = Substitute.For<IQueryHandler<TestQuery, int>>();
        var transactionExecutor = Substitute.For<ICommandTransactionExecutor>();
        var cancellationToken = new CancellationTokenSource().Token;

        queryHandler
            .HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(42);

        services.AddSingleton(queryHandler);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new RequestDispatcher(serviceProvider, transactionExecutor);

        var result = await dispatcher.SendQueryAsync<TestQuery, int>(new TestQuery("checkout-service"), cancellationToken);

        Assert.Equal(42, result);
        await queryHandler.Received(1)
            .HandleAsync(Arg.Is<TestQuery>(q => q.ApplicationName == "checkout-service"), cancellationToken);
        await transactionExecutor.DidNotReceiveWithAnyArgs()
            .ExecuteAsync<int>(default!, default);
    }

    public sealed record TestCommand(string Value) : ICommand<string>;

    public sealed record TestQuery(string ApplicationName) : IQuery<int>;
}
