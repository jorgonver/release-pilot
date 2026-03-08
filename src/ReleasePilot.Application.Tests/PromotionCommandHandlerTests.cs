using NSubstitute;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions.Commands;
using ReleasePilot.Api.Domain.Primitives;
using ReleasePilot.Api.Domain.Promotions;
using ReleasePilot.Api.Domain.Promotions.Events;

namespace ReleasePilot.Application.Tests;

public class PromotionCommandHandlerTests
{
    [Fact]
    public async Task RequestPromotionCommandHandler_AddsPromotionAndDispatchesRequestedEvent()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var handler = new RequestPromotionCommandHandler(repository, eventDispatcher);
        var cancellationToken = new CancellationTokenSource().Token;

        repository.ListAsync(cancellationToken).Returns(Array.Empty<Promotion>());

        var command = new RequestPromotionCommand(
            ApplicationName: "checkout-service",
            Version: "1.2.3",
            SourceEnvironment: "dev",
            TargetEnvironment: "staging",
            ActingUser: "requester-user",
            WorkItems:
            [
                new RequestPromotionWorkItemInput("WI-123", "Fix timeout")
            ]);

        var result = await handler.HandleAsync(command, cancellationToken);

        Assert.Equal("Requested", result.Status);
        await repository.Received(1)
            .AddAsync(Arg.Is<Promotion>(p =>
                p.ApplicationName == "checkout-service" &&
                p.Version == "1.2.3" &&
                p.Status == PromotionStatus.Requested), cancellationToken);
        await eventDispatcher.Received(1)
            .DispatchAsync(Arg.Is<IReadOnlyCollection<IDomainEvent>>(events =>
                events.Count == 1 && events.First() is PromotionRequestedDomainEvent), cancellationToken);
    }

    [Fact]
    public async Task ApprovePromotionCommandHandler_WhenPromotionNotFound_ThrowsKeyNotFoundException()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var handler = new ApprovePromotionCommandHandler(repository, eventDispatcher);
        var promotionId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        repository.GetByIdAsync(promotionId, cancellationToken).Returns((Promotion?)null);

        var action = () => handler.HandleAsync(new ApprovePromotionCommand(promotionId, "Approver", "approver-user"), cancellationToken);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        Assert.Equal($"Promotion '{promotionId}' was not found.", exception.Message);
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await eventDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task StartDeploymentCommandHandler_StartsDeployment_UpdatesPromotion_AndDispatchesEvent()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var deploymentPort = Substitute.For<IDeploymentPort>();
        var handler = new StartDeploymentCommandHandler(repository, eventDispatcher, deploymentPort);
        var cancellationToken = new CancellationTokenSource().Token;

        var promotion = CreateApprovedPromotion();
        promotion.PullDomainEvents();
        repository.GetByIdAsync(promotion.Id, cancellationToken).Returns(promotion);
        repository.ListAsync(cancellationToken).Returns(new[] { promotion });

        var result = await handler.HandleAsync(new StartDeploymentCommand(promotion.Id, "deployer-user"), cancellationToken);

        Assert.Equal("InProgress", result.Status);
        await deploymentPort.Received(1).StartDeploymentAsync(
            Arg.Is<DeploymentRequest>(request =>
                request.PromotionId == promotion.Id
                && request.ApplicationName == promotion.ApplicationName
                && request.Version == promotion.Version
                && request.SourceEnvironment == promotion.SourceEnvironment
                && request.TargetEnvironment == promotion.TargetEnvironment),
            cancellationToken);
        await repository.Received(1).UpdateAsync(Arg.Is<Promotion>(p => p.Status == PromotionStatus.InProgress), cancellationToken);
        await eventDispatcher.Received(1)
            .DispatchAsync(Arg.Is<IReadOnlyCollection<IDomainEvent>>(events =>
                events.Count == 1 && events.First() is DeploymentStartedDomainEvent), cancellationToken);
    }

    [Fact]
    public async Task StartDeploymentCommandHandler_WhenPromotionNotFound_ThrowsKeyNotFoundException()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var deploymentPort = Substitute.For<IDeploymentPort>();
        var handler = new StartDeploymentCommandHandler(repository, eventDispatcher, deploymentPort);
        var promotionId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        repository.GetByIdAsync(promotionId, cancellationToken).Returns((Promotion?)null);

        var action = () => handler.HandleAsync(new StartDeploymentCommand(promotionId, "deployer-user"), cancellationToken);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        Assert.Equal($"Promotion '{promotionId}' was not found.", exception.Message);
        await deploymentPort.DidNotReceiveWithAnyArgs().StartDeploymentAsync(default!, default);
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await eventDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task CompletePromotionCommandHandler_CompletesPromotion_UpdatesAndDispatchesEvent()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var handler = new CompletePromotionCommandHandler(repository, eventDispatcher);
        var cancellationToken = new CancellationTokenSource().Token;

        var promotion = CreateInProgressPromotion();
        promotion.PullDomainEvents();
        repository.GetByIdAsync(promotion.Id, cancellationToken).Returns(promotion);

        var result = await handler.HandleAsync(new CompletePromotionCommand(promotion.Id, "deployer-user"), cancellationToken);

        Assert.Equal("Completed", result.Status);
        await repository.Received(1).UpdateAsync(Arg.Is<Promotion>(p => p.Status == PromotionStatus.Completed), cancellationToken);
        await eventDispatcher.Received(1)
            .DispatchAsync(Arg.Is<IReadOnlyCollection<IDomainEvent>>(events =>
                events.Count == 1 && events.First() is PromotionCompletedDomainEvent), cancellationToken);
    }

    [Fact]
    public async Task CompletePromotionCommandHandler_WhenPromotionNotFound_ThrowsKeyNotFoundException()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var handler = new CompletePromotionCommandHandler(repository, eventDispatcher);
        var promotionId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        repository.GetByIdAsync(promotionId, cancellationToken).Returns((Promotion?)null);

        var action = () => handler.HandleAsync(new CompletePromotionCommand(promotionId, "deployer-user"), cancellationToken);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        Assert.Equal($"Promotion '{promotionId}' was not found.", exception.Message);
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await eventDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task RollbackPromotionCommandHandler_RollsBackPromotion_UpdatesAndDispatchesEvent()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var handler = new RollbackPromotionCommandHandler(repository, eventDispatcher);
        var cancellationToken = new CancellationTokenSource().Token;

        var promotion = CreateInProgressPromotion();
        promotion.PullDomainEvents();
        repository.GetByIdAsync(promotion.Id, cancellationToken).Returns(promotion);

        var result = await handler.HandleAsync(new RollbackPromotionCommand(promotion.Id, "deployment failed", "deployer-user"), cancellationToken);

        Assert.Equal("RolledBack", result.Status);
        await repository.Received(1)
            .UpdateAsync(Arg.Is<Promotion>(p =>
                p.Status == PromotionStatus.RolledBack && p.RolledBackReason == "deployment failed"), cancellationToken);
        await eventDispatcher.Received(1)
            .DispatchAsync(Arg.Is<IReadOnlyCollection<IDomainEvent>>(events =>
                events.Count == 1 && events.First() is PromotionRolledBackDomainEvent), cancellationToken);
    }

    [Fact]
    public async Task RollbackPromotionCommandHandler_WhenPromotionNotFound_ThrowsKeyNotFoundException()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var handler = new RollbackPromotionCommandHandler(repository, eventDispatcher);
        var promotionId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        repository.GetByIdAsync(promotionId, cancellationToken).Returns((Promotion?)null);

        var action = () => handler.HandleAsync(new RollbackPromotionCommand(promotionId, "deployment failed", "deployer-user"), cancellationToken);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        Assert.Equal($"Promotion '{promotionId}' was not found.", exception.Message);
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await eventDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task CancelPromotionCommandHandler_CancelsPromotion_UpdatesAndDispatchesEvent()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var handler = new CancelPromotionCommandHandler(repository, eventDispatcher);
        var cancellationToken = new CancellationTokenSource().Token;

        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();
        repository.GetByIdAsync(promotion.Id, cancellationToken).Returns(promotion);

        var result = await handler.HandleAsync(new CancelPromotionCommand(promotion.Id, "requester-user"), cancellationToken);

        Assert.Equal("Cancelled", result.Status);
        await repository.Received(1).UpdateAsync(Arg.Is<Promotion>(p => p.Status == PromotionStatus.Cancelled), cancellationToken);
        await eventDispatcher.Received(1)
            .DispatchAsync(Arg.Is<IReadOnlyCollection<IDomainEvent>>(events =>
                events.Count == 1 && events.First() is PromotionCancelledDomainEvent), cancellationToken);
    }

    [Fact]
    public async Task CancelPromotionCommandHandler_WhenPromotionNotFound_ThrowsKeyNotFoundException()
    {
        var repository = Substitute.For<IPromotionRepository>();
        var eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        var handler = new CancelPromotionCommandHandler(repository, eventDispatcher);
        var promotionId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        repository.GetByIdAsync(promotionId, cancellationToken).Returns((Promotion?)null);

        var action = () => handler.HandleAsync(new CancelPromotionCommand(promotionId, "requester-user"), cancellationToken);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        Assert.Equal($"Promotion '{promotionId}' was not found.", exception.Message);
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await eventDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    private static Promotion CreateRequestedPromotion()
    {
        return Promotion.Create(
            applicationName: "checkout-service",
            version: "1.2.3",
            sourceEnvironment: "dev",
            targetEnvironment: "staging",
            actingUser: "requester-user");
    }

    private static Promotion CreateApprovedPromotion()
    {
        var promotion = CreateRequestedPromotion();
        promotion.PullDomainEvents();
        promotion.Approve("Approver", "approver-user");
        return promotion;
    }

    private static Promotion CreateInProgressPromotion()
    {
        var promotion = CreateApprovedPromotion();
        promotion.PullDomainEvents();
        promotion.Start("deployer-user");
        return promotion;
    }
}
