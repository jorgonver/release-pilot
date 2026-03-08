using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Infrastructure.Ports;

public sealed class NoOpDeploymentPort : IDeploymentPort
{
    private readonly ILogger<NoOpDeploymentPort> _logger;

    public NoOpDeploymentPort(ILogger<NoOpDeploymentPort> logger)
    {
        _logger = logger;
    }

    public Task StartDeploymentAsync(DeploymentRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "NoOp deployment invoked for promotion {PromotionId} ({ApplicationName} {Version}: {Source}->{Target})",
            request.PromotionId,
            request.ApplicationName,
            request.Version,
            request.SourceEnvironment,
            request.TargetEnvironment);

        return Task.CompletedTask;
    }
}
