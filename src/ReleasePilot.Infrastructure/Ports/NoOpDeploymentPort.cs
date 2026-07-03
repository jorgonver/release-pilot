using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Infrastructure.Logging;

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
        InfrastructureLogMessages.NoOpDeploymentInvoked(
            _logger,
            request.PromotionId,
            request.ApplicationName,
            request.Version,
            request.SourceEnvironment,
            request.TargetEnvironment);

        return Task.CompletedTask;
    }
}
